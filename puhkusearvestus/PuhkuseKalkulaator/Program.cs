using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Program
{
    private static List<int[]> times = new List<int[]>();
    private static List<int> timeList = new List<int>();
    private static Dictionary<int, int> countDict = new Dictionary<int, int>();
    private static List<int[]> filteredTimes = new List<int[]>();
    public static void Main()
    {
        bool keepRunning = true;
        int userSpecified = 3; 

        while (keepRunning)
        {
            Console.WriteLine("Choose an action:");
            Console.WriteLine("1. Add times");
            Console.WriteLine("2. Increase/decrease scope (-/+)");
            Console.WriteLine("3. Check results");
            Console.WriteLine("4. Exit");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    AddTimes();
                    break;

                case "2":
                    Console.WriteLine($"Current value: {userSpecified}");
                    Console.WriteLine("Type '-' to decrease or '+' to increase (1-5):");
                    string input = Console.ReadLine();
                    if (input == "-") userSpecified--;
                    else if (userSpecified < 1) userSpecified = 1;
                    else if (input == "+") userSpecified++;
                    else if (userSpecified > 5) userSpecified = 5;
                    else Console.WriteLine("Invalid input. Try again."); 
                    break;

                case "3":
                    RunCalculations(userSpecified);
                    break;

                case "4":
                case "exit":
                case "Exit":
                    keepRunning = false;
                    break;

                default:
                    Console.WriteLine("Invalid choice. Try again.");
                    break;
            }
        }
    }

    static void AddTimes()
    {
        Console.WriteLine("Enter the time range (format HH:mmHH:mm) or 'back' to return:");
        string timeRange = Console.ReadLine();
        if (timeRange == "back") return;

        if (!IsValidTimeRange(timeRange))
        {
            Console.WriteLine("Invalid time format. Try again.");
            return;
        }

        using (StreamWriter sw = File.AppendText("times.txt"))
        {
            sw.WriteLine(timeRange);
        }
        Console.WriteLine("Time range added.");
    }

    static bool IsValidTimeRange(string timeRange)
    {
        if (timeRange.Length != 10) return false;
        if (timeRange[2] != ':' || timeRange[7] != ':') return false;
        return true;
    }

    static void RunCalculations(int userSpecified)
    {

        ReadAndFormatFileIntoTimesInMinutes(times);
        times.Sort((x, y) => x[0].CompareTo(y[0]));
        AddRangeToTimeList(0, 1441, timeList);
        PlotTimesAsMinutes(times, timeList);
        timeList.Sort();
        FilterAndCountMostPopulatedRegions(timeList, userSpecified);

    }



    static void ReadAndFormatFileIntoTimesInMinutes(List<int[]> times)
    {
        var input = File.ReadAllLines("times.txt");


        foreach (var line in input)
        {
            string[] startTime = line.Substring(0, 5).Split(':');
            string[] endTime = line.Substring(5, 5).Split(':');

            int startTimeInMinutes = int.Parse(startTime[0]) * 60 + int.Parse(startTime[1]);
            int endTimeInMinutes = int.Parse(endTime[0]) * 60 + int.Parse(endTime[1]);

            times.Add(new int[] { startTimeInMinutes, endTimeInMinutes });
        }
    }

    static void PlotTimesAsMinutes(List<int[]> times, List<int> timeList)

    {

        foreach (var x in times)
        {
            int startTimeX = x[0];
            int endTimeX = x[1];

            if (endTimeX < startTimeX)
            {
                AddRangeToTimeList(startTimeX, 1440, timeList);
                AddRangeToTimeList(0, endTimeX, timeList);

            }
            else
            {
                AddRangeToTimeList(startTimeX, endTimeX, timeList);
            }
        }

    }

    static void AddRangeToTimeList(int start, int end, List<int> timeList, int? exclusion = null)
    {
        for (int i = start; i <= end; i++)
        {
            if (i != exclusion)
            {
                timeList.Add(i);
            }
        }
    }

    private static Dictionary<int, int> GetTimeCounts(List<int> timeList)
    {
        return timeList.GroupBy(x => x).ToDictionary(group => group.Key, group => group.Count());
    }
    private static int GetMaxCount(Dictionary<int, int> countDict)
    {
        return countDict.Max(item => item.Value);
    }


    static void FilterAndCountMostPopulatedRegions(List<int> timeList, int userSpecified)
    {
        filteredTimes.Clear();
        countDict = GetTimeCounts(timeList);
        var maxCount = GetMaxCount(countDict);
        int min = 1441;
        int max = 0;
        List<int> spottedHighs = new List<int>();
        int amountOfTimesInRegion = 0;
        int precision = maxCount - userSpecified;


        while (maxCount > precision)       //Maxcount serves as the standard for locating most densely populated regions in the timeList and 
                                           // this condition can be adjusted to accommodate wider regions
        {

            maxCount--;

            foreach (var item in countDict)

            {

                if (item.Value >= maxCount)        //Finds regions that have at least as many overlaps as maxCount and locates starting and end points
                {                                  // of the region and stores the values in a list
                    if (spottedHighs.Count == 0)
                    {
                        min = item.Key;
                    }
                    max = item.Key;
                    spottedHighs.Add(item.Value);

                }

                else if (item.Value != maxCount && spottedHighs.Count > 1) // upon most densely populated region end, perform operations on than region
                {


                    if (min == 0) // upon highest values at start of day, check for midnight crossover 
                    {

                        HandleMidNight(countDict, maxCount, spottedHighs);
                        spottedHighs.Clear();

                    }
                    else     // for highest regions occurring during all other times of day
                    {
                        if (max == 1440 && countDict[0] >= maxCount)
                        {
                            continue;
                        }

                        amountOfTimesInRegion = CountAllTimesInAIntervall(maxCount, spottedHighs); // counts the various times in a region
                        filteredTimes.Add(new int[] { min, max, amountOfTimesInRegion });
                        spottedHighs.Clear();

                    }

                }

            }
        }

        PrintFilteredTimes();

    }


    static int CountAllTimesInAIntervall(int amountOfTimesInRegion, List<int> spottedHighs)  
    {
        int previous = amountOfTimesInRegion;
        foreach (var x in spottedHighs)           // if a region is not with a uniform density, the maxCount might not represent the actual amount of times. 
                                                  // by counting the increases in density within a region, the actual amount of times can be found 
        {

            if (x > previous)
            {

                amountOfTimesInRegion += x - previous;

            }
            previous = x;
        }
        return amountOfTimesInRegion;
    }

    static void HandleMidNight(Dictionary<int, int> countDict, int maxCount, List<int> spottedHighs)
    {

        int min = 1441;
        int max = 0;
        int x = 1440;
        int y = 0;
        int amountOfTimesInRegion = 0;
        List<int> midnightSpottedHighs = new List<int>();                   //the region that crosses midnight is plotted in a list and connected with the
                                                                            //spottedHighs list which stores the region values starting from start of day

        while (countDict[x] >= maxCount)
        {
            x--;

            midnightSpottedHighs.Add(countDict[x]);

        }
        min = x + 1;

        midnightSpottedHighs.Reverse();
        midnightSpottedHighs.AddRange(spottedHighs);
        midnightSpottedHighs.RemoveAt(0);


        while (countDict[y] >= maxCount)
        {
            y++;

        }

        max = y - 1;
        amountOfTimesInRegion = CountAllTimesInAIntervall(maxCount, midnightSpottedHighs);
        filteredTimes.Add(new int[] { min, max, amountOfTimesInRegion });

    }


    static void PrintFilteredTimes()
    {

        filteredTimes.Sort((x, y) => y[2].CompareTo(x[2]));

        for (int i = 0; i < 1; i++)
        {
            int[] time = filteredTimes[i];
            int startTime = time[0];
            int endTime = time[1];
            int countTime = time[2];
            int startHourTime = startTime / 60;
            if (startHourTime == 24)
            {
                startHourTime = 0;
            }
            int startMinTime = startTime % 60;
            int endHourTime = endTime / 60;
            if (endHourTime == 24)
            {
                endHourTime = 0;
            }
            int endMinTime = endTime % 60;
            Console.WriteLine($"Results: \n \n{countTime} people are resting between {startHourTime:D2}:{startMinTime:D2} and {endHourTime:D2}:{endMinTime:D2} \n");
            timeList.Clear();
            times.Clear();

        }

    }
}