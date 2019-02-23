using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OfficeOpenXml;

namespace GASeatingProblem.Seating
{
    /// <summary>
    /// This evaluator grades person-to-desk layouts, based on rules such as conflict avoidance, team preference, and assigned seats.
    /// </summary>
    public class LayoutEvaluator : ILayoutEvaluator<int>
    {
        public Random RNG = GlobalRandom.GetLocalRNG();
        public static DateTime targetDate = DateTime.Now;

        public static double GlobalBestFitness;
        public static LayoutChromosome GlobalBestChromosome;


        /// <summary>
        /// A map of employees by index. Populated once, and then used for quick lookups.
        /// Each person knows where they belong in the array, so conflicts are equally fast to find.
        /// </summary>
        static Person[] employees;

        private static int _emptyDesks = -1;
        public static int EmptyDesks
        {
            get
            {
                if (_emptyDesks != -1) return _emptyDesks;
                _emptyDesks = employees.Count(person => person.Team == 0);
                return _emptyDesks;
            }
        }
        
        public int TotalEmployees => employees.Length;

        /// <summary>
        /// How many employees are present today, that where also present yesterday?
        /// </summary>
        private static int TotalCarryOverEmployees = 0;

        /// <summary>
        /// The desks converted from a 2D structure to a 1D array.
        /// Also only populated once. Each Desk has a ToIndex() converter, so also knows where it belongs
        /// in this collection.
        /// Desks also have a DistanceTo(otherDesk) method, to allow for coflict/team measurements.
        /// </summary>
        static Desk[] desks;

        /// <summary>
        /// The target layout is used to count how many people are in the same seat as the day before.
        /// When null, the score won't be counted, and no target will be used.
        /// To accomodate the fact that the number of users present varies each day:
        /// TODO - add a shared ratio calculated once, that counts the number of new vs return users.
        ///        only need to do this if a simple percentage doesn't give good fitness results.
        /// </summary>
        public static LayoutChromosome TargetLayout;

        public double GetFitness(IChromosome<int> chromosome)
        {
            return GetFitness((LayoutChromosome) chromosome);
        }

        public double GetFitness(LayoutChromosome chromosome)
        {
            //Notice that we DON'T call the duplicateCounter method. This is because (at least in this version), duplication should be
            //impossible. This is becuase the breeding steps include duplication removal as one of the final steps.

            //Primary measure of fitness will be the ratio of total conflict, to max-possible conflicts.
            var conflictCounts = CountInRangeConflicts(chromosome);
            double maxConflicts = 0;
            double currentConflict = 0;
            double currentOtherTeamDeskSitters = 0;
            double priorSeatMatches = 0;

            var targetSeats = TargetLayout?.GetRawElements();
            var personIndexes = chromosome.GetRawElements();
            for (int i = 0; i < conflictCounts.Length; i++)
            {
                Person currentPerson = employees[personIndexes[i]];
                maxConflicts += currentPerson.Conflicts.Count;
                currentConflict += conflictCounts[i];
                if (desks[i].TeamNumber != 0 && //The desk isn't a shared desk
                    currentPerson.Team != 0 &&  //The person cares about a team (0 means doesn't care)
                    currentPerson.Team != desks[i].TeamNumber) //The desk and person match team-assignments
                {
                    currentOtherTeamDeskSitters++;
                }

                //Only if there is a target layout, count the number of seats that match the previous day.
                if (targetSeats != null && targetSeats[i] >= 0)
                {
                    //If target seats is -1, then it represent an desk that was either empty, or whose user didn't show up today - so no comparison should be done
                    priorSeatMatches += currentPerson.Index == employees[targetSeats[i]].Index ? 1 : 0;
                }
            }
            
            
            var percetageOfConflictsActive = currentConflict / maxConflicts;
            
            //"Empty desks" (implemented as people with the same same and no team)
            //Should not count towards the total number of people being placed in right/wrong desks (it reduced the max fitness by whatever percentage of desks are empty).
            var percetageNotInTeamOrSharedSeats = currentOtherTeamDeskSitters / (desks.Length-EmptyDesks);

            //Default to 100% of people being in their prior seats. Overwrite that if there are actually any carry-over employees
            double percetageInPreviousSeat = 1;
            if (TotalCarryOverEmployees != 0)
                percetageInPreviousSeat = priorSeatMatches / TotalCarryOverEmployees;

            chromosome.conflictPercentage = percetageOfConflictsActive;
            chromosome.historicSeatingUsedPercentage = percetageInPreviousSeat;
            chromosome.teamSeatsUsedPercentage = 1 - percetageNotInTeamOrSharedSeats;

            ////Could extend this patter to:
            //if (percetageOfConflictsActive == 0 && someSecondaryMeasure) return 100+tertiaryFitness;

            if (percetageOfConflictsActive == 0)
            {
                //Give a big bonus to legal groupings, but also factor in their team-arrangments
                return 10 + ((1 - percetageNotInTeamOrSharedSeats) + percetageInPreviousSeat) * 0.5;
            }
            //Avoiding conflict is considered twice as important as sitting with your team or in a previous seat (hence the X*0.5)
            return (1 - percetageOfConflictsActive) + (chromosome.teamSeatsUsedPercentage + percetageInPreviousSeat)*0.5;
        }

        public bool IsValid(IChromosome<int> chromosome)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Office users can be removed from the list of employees that need assigned desks.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private List<string> GetOfficeUsersFromBothTeams(string filename)
        {
            var officedEmployees = new List<string>();

            using (var package = new ExcelPackage(new FileInfo(filename)))
            {
                ExcelWorksheet checkInSheet = null;
                //select the correct worksheet
                foreach (var sheet in package.Workbook.Worksheets)
                {
                    if (!sheet.Name.EndsWith("Off")) continue;

                    checkInSheet = sheet;
                    var rowCount = checkInSheet.Dimension.End.Row;
                    for (var row = 1; row <= rowCount; row++)
                    {
                        var name = checkInSheet.Cells[row, 1].Value.ToString().Trim();
                        officedEmployees.Add(name);
                    }
                }
            }

            return officedEmployees;
        }

        private List<string> GetCheckedInEmployees(string filename)
        {
            var checkedInEmployees = new List<string>();

            using (var package = new ExcelPackage(new FileInfo(filename)))
            {
                ExcelWorksheet checkInSheet = null;
                //select the correct worksheet
                foreach (var sheet in package.Workbook.Worksheets)
                {
                    if (sheet.Name != "CheckIn") continue;

                    checkInSheet = sheet;
                    break;
                }
                
                var rowCount = checkInSheet.Dimension.End.Row;     //get row count
                for (int row = 2; row <= rowCount; row++)
                {
                    //To ignore the time on these simply call .Date on each one after parsing
                    var startTime = DateTime.Parse(checkInSheet.Cells[row, 2].Value.ToString().Trim()).Date;
                    var endTime = DateTime.Parse(checkInSheet.Cells[row, 3].Value.ToString().Trim()).Date;
                    if (targetDate >= startTime && targetDate <= endTime)
                    {
                        var name = checkInSheet.Cells[row, 1].Value.ToString().Trim();
                        checkedInEmployees.Add(name);
                    }
                }
            }

            return checkedInEmployees;
        }

        private List<string> GetTeamMemberNames(string filename, string teamTabName)
        {
            var teamNames = new List<string>();

            using (ExcelPackage package = new ExcelPackage(new FileInfo(filename)))
            {
                ExcelWorksheet checkInSheet = null;
                //select the correct worksheet
                foreach (var sheet in package.Workbook.Worksheets)
                {
                    if (sheet.Name != teamTabName) continue;

                    checkInSheet = sheet;
                    break;
                }
                
                var rowCount = checkInSheet.Dimension.End.Row;     //get row count
                for (int row = 1; row <= rowCount; row++)
                {
                    var name = checkInSheet.Cells[row, 1].Value.ToString().Trim();
                    teamNames.Add(name);
                }
            }

            return teamNames;
        }

        private Dictionary<string, List<string>> GetConflictDictionary(string filename)
        {
            var conflicts = new Dictionary<string, List<string>>();

            using (ExcelPackage package = new ExcelPackage(new FileInfo(filename)))
            {
                ExcelWorksheet checkInSheet = null;
                //select the correct worksheet
                foreach (var sheet in package.Workbook.Worksheets)
                {
                    if (sheet.Name != "Conflicts") continue;

                    checkInSheet = sheet;
                    break;
                }

                var rowCount = checkInSheet.Dimension.End.Row;     //get row count
                for (int row = 2; row <= rowCount; row++)
                {
                    var name = checkInSheet.Cells[row, 1].Value.ToString().Trim();
                    var conflictorString = checkInSheet.Cells[row, 2].Value.ToString().Trim();
                    
                    conflicts.Add(name, conflictorString.Replace("#", "").Split(';').Where(x => x.Length > 4).ToList());
                }
            }

            return conflicts;
        }

        private void InitialiseDesksFromFile(string fileName)
        {
            List<Desk> tempDesks = new List<Desk>();
            
            using (ExcelPackage package = new ExcelPackage(new FileInfo(fileName)))
            {
                ExcelWorksheet deskSheet = null;
                //select the correct worksheet
                foreach (var sheet in package.Workbook.Worksheets)
                {
                    if (sheet.Name != "Desks") continue;

                    deskSheet = sheet;
                    break;
                }

                var rowCount = deskSheet.Dimension.End.Row;     //get row count
                for (int row = 2; row <= rowCount; row++)
                {
                    var open = deskSheet.Cells[row, 6].Value.ToString().Trim().ToLower();
                    string team = deskSheet.Cells[row, 7]?.Value?.ToString().Trim().ToLower();

                    //No adding of desks that don't belong to our teams
                    if (string.IsNullOrWhiteSpace(team))
                        continue;

                    var deskCol = int.Parse(deskSheet.Cells[row, 3].Value.ToString().Trim());
                    var deskRow = int.Parse(deskSheet.Cells[row, 4].Value.ToString().Trim());

                    var newDesk = new Desk(deskRow, deskCol);
                    newDesk.TeamNumber = team == "team1" ? 1 : 2;
                    tempDesks.Add(newDesk);
                }
                
                //Make sure the desks are in the expected order, or the employee-to-desk mapping won't work
                desks = tempDesks.OrderBy(desk => desk.GetIndex()).ToArray();
            }
        }

        public static void LoadPriorSeating(string filename)
        {
            if(employees == null) throw new Exception("You need to have loaded today's employees prior to loading historic data");
            
            PriorLayout prior = JsonConvert.DeserializeObject<PriorLayout>(File.ReadAllText(filename));

            //Create an index map. This map will be able to say things like "Our current Nth employee, is your Yth employee", or "Our Nth employee is was not hear yesterday".
            //Index maps start out with -1 values, so that if we can't find a match it shows up easily.
            //int[] newToOld = new int[employees.Length].Select(x => -1).ToArray();
            int[] oldToNew = new int[prior.Employees.Length].Select(x => -1).ToArray();


            for (var j = 0; j < prior.Employees.Length; j++)
            {
                var foundPartner = false;
                for (var i = 0; i < employees.Length; i++)
                {
                    //Notice we don't check if the indexes match - as those are what change each day.
                    if (employees[i].Name != prior.Employees[j].Name ||
                        employees[i].Team != prior.Employees[j].Team) continue;

                    if (employees[i].Index != i)
                        Console.WriteLine("Warning: indexes are mismatched.");

                    //if it's an empty desk, then mark is as 'unasigned' by leaving it with a -1 value
                    oldToNew[j] = employees[i].Name == "EMPTY DESK" ? -1 : i;
                    foundPartner = true;
                    break;
                }
                if(!foundPartner)
                    Console.WriteLine($"No partner for {j}");
            }

            //Go over the old chromosome, and remap indexes to use our current employee data.
            //Empty desks, and people that didn't show up, will be given values of -1 (which will later mean "no prefered sitter")
            var newRawChrome = new int[prior.RawChromosomeIndexes.Count];
            for (int i = 0; i < prior.RawChromosomeIndexes.Count; i++)
            {
                int oldPersonIndex = prior.RawChromosomeIndexes[i];
                if (oldToNew[oldPersonIndex] == -1)
                {
                    //The old person didn't show up to work today, or it's an empty desk, so mark their desk (in the chromosome), as empty/free
                    newRawChrome[i] = -1;
                    continue;
                }

                //getting here means we do have a match, so change the rawChromosomeIndex to use the new value
                newRawChrome[i] = oldToNew[prior.RawChromosomeIndexes[i]];
            }

            TotalCarryOverEmployees = newRawChrome.Count(x => x >= 0);
            TargetLayout = new LayoutChromosome();
            TargetLayout.InitialiseFromRawInputs(newRawChrome.ToList());
        }
        
        public static void SaveForFuturePriorSeating(string filename)
        {
            var toSave = new PriorLayout
            {
                Desks = desks,
                Employees = employees.Select(person => new Person(person.Name, person.Index, person.Team)).ToArray(),
                RawChromosomeIndexes = GlobalBestChromosome.GetRawElements()
            };

            var asJson = JsonConvert.SerializeObject(toSave, Formatting.Indented);
            File.WriteAllText(filename, asJson);
        }

        public void InitialiseFromFile(string fileName)
        {
            //Create the desks to be used for distance-checking and team-membership.
            InitialiseDesksFromFile(fileName);

            ////BEGIN LOADING PERSONS
            var checkInNames = GetCheckedInEmployees(fileName);
            var officeUsers = GetOfficeUsersFromBothTeams(fileName);
            foreach (var name in officeUsers)
            {
                checkInNames.Remove(name);
            }
            var team1Names = GetTeamMemberNames(fileName, "Team1");
            var team2Names = GetTeamMemberNames(fileName, "Team2");

            var tempEmployees = new List<Person>();
            int totalCheckedInEmployees = checkInNames.Count;
            for (int i = 0; i < totalCheckedInEmployees; i++)
            {
                var currentName = checkInNames[i];
                var person = new Person(checkInNames[i], i, team1Names.Contains(currentName) ? 1 : 2);
                tempEmployees.Add(person);
            }
            
            //Fill in empty space with EMPTY DESK "emplyees" - that will have zero conflicts.
            while (tempEmployees.Count < desks.Length)
            {
                var person = new Person($"EMPTY DESK", tempEmployees.Count, 0);
                tempEmployees.Add(person);
            }

            employees = tempEmployees.ToArray();


            /////ADD CONFLICTS
            var conflictNames = GetConflictDictionary(fileName);
            //Note that we only add up until totalRealEmployees - that way any "empty desk" employees don't get conflicts added
            for (var i = 0; i < totalCheckedInEmployees; i++)
            {
                var name = checkInNames[i];
                //You found a 'get along with everyone' person. Skip over them, for they are worthy.
                if (!conflictNames.ContainsKey(name)) continue;

                var conflictors = conflictNames[name];
                foreach (var enemy in conflictors)
                {
                    var enemyEmployeeIndex = checkInNames.FindIndex(x => x == enemy);

                    //If you couldn't find the enemy, they must not have checked in today, so don't add them
                    if(enemyEmployeeIndex == -1) continue;

                    employees[i].TryAddConflict(employees[enemyEmployeeIndex]);
                }
            }
        }

        /// <summary>
        /// Goes over each desk, and counts how many conflicting persons are within range of each desk.
        /// There should naturally/normally be fewer on the extremeties - as they have fewer desks on one side of them.
        /// </summary>
        /// <returns></returns>
        public static int[] CountInRangeConflicts(LayoutChromosome layout)
        {
            var conflictCounter = new int[desks.Length];
            var personIDs = layout.GetRawElements();
            Person currentPerson;
            Person neighbor;
            Desk currentDesk;
            for (int deskIndex = 0; deskIndex < desks.Length; deskIndex++)
            {
                currentPerson = employees[personIDs[deskIndex]];
                currentDesk = desks[deskIndex];

                //No conflictors means we'll only forward once... 10 conflictors means 10 seperate lookaheads :/
                foreach (var conflictor in currentPerson.Conflicts)
                {
                    for (var neighborIndex = deskIndex + 1; neighborIndex < desks.Length; neighborIndex++)
                    {
                        neighbor = employees[personIDs[neighborIndex]];
                        if (neighbor == conflictor)
                        {
                            int seperators = currentDesk.countSeperatorsBetweenDesks(desks[neighborIndex].Row);
                            //2 or more seperators, means no conflict, so keep looking
                            if (seperators >= 2) continue;

                            //Both people get counted as enemies.
                            conflictCounter[deskIndex]++;
                            conflictCounter[neighborIndex]++;
                        }
                    }
                }
            }

            return conflictCounter;
        }

        /// <summary>
        /// Go over the given layout, and count how many times each person of a certain ID is present.
        /// 0 is fine, and 1 is fine. We want to use the resulting array to a. check for duplicate persons and b. swap out duplicate people with persons of count 0
        ///
        /// The return array is a 1-to-1 map of the complete employee list (not just those in the given layout).
        /// </summary>
        /// <param name="layout"></param>
        /// <returns></returns>
        public static int[] CountEmployeeOccurences(LayoutChromosome layout)
        {
            var occurenceCounts = new int[employees.Length];
            foreach (var employeeIdx in layout.GetRawElements())
            {
                occurenceCounts[employeeIdx]++;
            }

            return occurenceCounts;
        }

        public static void DisplayEmployeeConflicts()
        {
            var toDisplay = new StringBuilder();
            foreach (var person in employees)
            {
                toDisplay.Append($"Person {person.Name} conflicts with: ");
                foreach (var conflictor in person.Conflicts)
                {
                    toDisplay.Append(conflictor.Name + ". ");
                }

                toDisplay.AppendLine();
            }
            Console.WriteLine(toDisplay.ToString());
        }

        /// <summary>
        /// Counts all conflicts, and displays the given layout in per-desk order.
        /// </summary>
        /// <param name="layout"></param>
        public static void DisplayLayoutFromChromosome(LayoutChromosome layout)
        {
            var personIDs = layout.GetRawElements();
            var conflictCounts = CountInRangeConflicts(layout);
            var occurenceCounts = CountEmployeeOccurences(layout);

            Person currentPerson;
            Desk currentDesk;
            StringBuilder toDisplay = new StringBuilder();
            for (int i = 0; i < desks.Length; i++)
            {
                currentPerson = employees[personIDs[i]];
                bool duplicate = occurenceCounts[personIDs[i]] > 1;

                currentDesk = desks[i];
                if (currentDesk.Col == 1)
                {
                    toDisplay.AppendLine();
                    if (currentDesk.Row <= 30)
                        toDisplay.Append($"{(currentDesk.Row % 2 == 0 ? "\n" : "")}Row {currentDesk.Row}: ");
                    else
                        toDisplay.Append($"{(currentDesk.Row % 2 == 1 ? "\n" : "")}Row {currentDesk.Row}: ");
                }

                toDisplay.Append(
                    $"{currentPerson.Name}{(duplicate ? "*" : " ")} (idx: {currentPerson.Index}, conflicts: {conflictCounts[i]}/{currentPerson.Conflicts.Count})  | ");
            }
            Console.WriteLine(toDisplay.ToString());
            Console.WriteLine("* these individuals have been assigned to multiple desks");
        }
    }

    class PriorLayout
    {
        public Person[] Employees;
        public Desk[] Desks;
        public List<int> RawChromosomeIndexes;
    }
}
