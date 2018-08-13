using System;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.IO;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.IO;
using Netduino.Foundation;
using Netduino.Foundation.RTCs;

namespace Automated_Relay_System{

        public class Program{

        static bool timeMode = true;
        static int waterLimit = 50,ldrLimit = 10,motionLimit = 100;
        static OutputPort relay01, relay02, relay03, relay04, relay05, relay06, relay07, relay08;
        public static void Main(){

            //Input Sensors
            Microsoft.SPOT.Hardware.AnalogInput WaterLevel = new Microsoft.SPOT.Hardware.AnalogInput(Cpu.AnalogChannel.ANALOG_0);
            Microsoft.SPOT.Hardware.AnalogInput LDR = new Microsoft.SPOT.Hardware.AnalogInput(Cpu.AnalogChannel.ANALOG_1);
            Microsoft.SPOT.Hardware.AnalogInput MotionDetection = new Microsoft.SPOT.Hardware.AnalogInput(Cpu.AnalogChannel.ANALOG_2);

            //Output Ports for Relays
            relay01 = new OutputPort(Pins.GPIO_PIN_D5, true);
            relay02 = new OutputPort(Pins.GPIO_PIN_D6, true);
            relay03 = new OutputPort(Pins.GPIO_PIN_D7, true);
            relay04 = new OutputPort(Pins.GPIO_PIN_D8, true);
            relay05 = new OutputPort(Pins.GPIO_PIN_D9, true);
            relay06 = new OutputPort(Pins.GPIO_PIN_D10, true);
            relay07 = new OutputPort(Pins.GPIO_PIN_D11, true);
            relay08 = new OutputPort(Pins.GPIO_PIN_D12, true);

            //DS3231 definition
            DS3231 rtc = new DS3231(0x68, 100, Pins.GPIO_PIN_D3);

            while (true){
                //Getting current time from DS3231
                int year = (int)rtc.CurrentDateTime.Year;
                int month = (int)rtc.CurrentDateTime.Month;
                int day = (int)rtc.CurrentDateTime.Day;
                int hour = (int)rtc.CurrentDateTime.Hour;
                int minute = (int)rtc.CurrentDateTime.Minute;
                int second = (int)rtc.CurrentDateTime.Second;
                int dayOfWeek = (int)rtc.CurrentDateTime.DayOfWeek;

                //If year is wrong
                if (year < 2000){
                    year += 100;
                }

                //Looking sensor values to decide relay states
                sensorTest(WaterLevel, LDR, MotionDetection);


                //Relay states are going to be decided by Time Program
                if (timeMode == true){
                    //SD Card Read Mode On
                    if (SDCardRead(1) != ""){

                        //Reading every relays' programs and activate them
                        for (int i = 1; i <= 8; i++) {
                            string[] relayAll = StringSplitter(SDCardRead(i));
                            TimeToAct(i, hour, minute, DayOfWeekToString(dayOfWeek), int.Parse(relayAll[1]),
                                int.Parse(relayAll[2]), int.Parse(relayAll[3]), int.Parse(relayAll[4]),
                                DetectDay(int.Parse(relayAll[5])));
                        }

                        //Write it from VS
                    } else {
                        // Relay no:1 14:10 - 14:59 Only Monday
                        TimeToAct(1, hour, minute, DayOfWeekToString(dayOfWeek), 14, 10, 14, 59, DetectDay(2));
                        SDCardWrite(1, 14, 10, 14, 59, 2); //It is a writing example

                        // Relay no:2 12:10 - 16:27 Only Tuesday
                        TimeToAct(2, hour, minute, DayOfWeekToString(dayOfWeek), 12, 10, 16, 27, DetectDay(3));

                        // Relay no:3 13:06 - 13:09 Only Thursday
                        TimeToAct(3, hour, minute, DayOfWeekToString(dayOfWeek), 13, 06, 13, 18, DetectDay(7));

                        // Relay no:4 09:25 - 18:17 Only Friday
                        TimeToAct(4, hour, minute, DayOfWeekToString(dayOfWeek), 09, 25, 18, 17, DetectDay(11));

                        // Relay no:5 10:00 - 12:00 Everyday
                        TimeToAct(5, hour, minute, DayOfWeekToString(dayOfWeek), 10, 00, 12, 00, DetectDay(510510));

                        // Relay no:6 04:35 - 14:40 Weekend (Saturday & Sunday)
                        TimeToAct(6, hour, minute, DayOfWeekToString(dayOfWeek), 04, 35, 14, 40, DetectDay(221));

                        // Relay no:7 12:15 - 12:46 Weekdays (Monday & Tuesday & Wednesday & Thursday & Friday)
                        TimeToAct(7, hour, minute, DayOfWeekToString(dayOfWeek), 12, 15, 12, 46, DetectDay(2310));

                        // Relay no:8 02:00 - 02:02 Monday & Wednesday & Friday & Sunday
                        TimeToAct(8, hour, minute, DayOfWeekToString(dayOfWeek), 02, 00, 02, 02, DetectDay(1870));
                    }
                }

                //Printing Current Time
                Debug.Print("Current Date: " + day + "/" + month + "/" + year);
                Debug.Print("Current Hour: " + hour + ":" + minute + "." + second);
                Debug.Print("Current Day of Week: " + DayOfWeekToString(dayOfWeek));

                //Printing Sensor Values
                Debug.Print("Water Level Sensor: " + (int)(WaterLevel.Read() * 100));
                Debug.Print("LDR Level Sensor: " + (int)(LDR.Read() * 100));
                Debug.Print("Motion Detection Sensor: " + (int)(MotionDetection.Read() * 100));


                Thread.Sleep(1000);
            }
            Thread.Sleep(Timeout.Infinite);
        }

        /// <summary>
        /// Split String from SD Card
        /// </summary>
        /// <param name="split"></param>
        /// <returns>retunr splitted string array</returns>
        static string[] StringSplitter(string split){
            string[] splitted;
            splitted = split.Split('.');
            return splitted;
        }

        /// <summary>
        /// SD Card Reader
        /// </summary>
        /// <param name="relayNo">Relay No</param>
        /// <returns></returns>
        static string SDCardRead(int relayNo)
        {
            string read = "";
            var volume = new VolumeInfo("SD");
            if (volume != null)
            {
                // "SD" is the volume name,
                var path = Path.Combine("SD", "Relay_" + relayNo + ".txt");
                if (File.Exists(path))
                {
                    StreamReader myFile = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read));
                    read = myFile.ReadLine();
                }
            }
            return read;
        }

        /// <summary>
        /// Write to SD Card and Read from SD Card
        /// </summary>
        /// <param name="relayNo"></param>
        /// <param name="openHour"></param>
        /// <param name="openMinute"></param>
        /// <param name="closeHour"></param>
        /// <param name="closeMinute"></param>
        /// <param name="days">days --just like below monday = 2 tuesday = 3 </param>
        /// <param name="write">true for writing</param>
        /// <returns>readLine</returns>
        static void SDCardWrite(int relayNo, int openHour, int openMinute, int closeHour, int closeMinute, int days)
        {
            var volume = new VolumeInfo("SD");

            //Writing from default ones from Visual Studio
            //Normally you can override it from your SD card

                if (volume != null)
                {
                    // "SD" is the volume name,
                    var path = Path.Combine("SD", "Relay_" + relayNo + ".txt");
                    string writingTo = relayNo + ".";
                    writingTo += openHour + ".";
                    writingTo += openMinute + ".";
                    writingTo += closeHour + ".";
                    writingTo += closeMinute + ".";
                    writingTo += days + ".";

                   // File.WriteAllBytes(path, Encoding.UTF8.GetBytes(writingTo));

                    StreamWriter myFile = new StreamWriter(path);
                    myFile.Write(writingTo);
                    myFile.Close();

                    volume.FlushAll();
                }else {
                    Debug.Print("Problem");
                }

        }
        /// <summary>
        /// Sensor Tests for Relay Control
        /// </summary>
        /// <param name="WaterLevel">Water Level Sensor</param>
        /// <param name="LDR">LDR Sensor</param>
        /// <param name="MotionDetection"> Motion Detection Sensor</param>
        static void sensorTest(AnalogInput WaterLevel,AnalogInput LDR, AnalogInput MotionDetection){
            if (!sensorAction((WaterLevel.Read() * 100), waterLimit))
            { //Stop the pumping water relay
                RelayController(RelaySelection(1), false); //In this case, 1st Relay will be closed
                timeMode = false;
            }
            else {
                RelayController(RelaySelection(1), true);
                timeMode = true;
            }

            if (sensorAction((LDR.Read() * 100), ldrLimit))
            { //If LDR shows value that is under 20, then close chicken's door
                RelayController(RelaySelection(4), true); //In this case, 4th Relay will be opened
                timeMode = false;
            }
            else {
                RelayController(RelaySelection(4), false);
                timeMode = true;
            }

            if (sensorAction((MotionDetection.Read() * 100), motionLimit))
            { //If motion is understood, then open the light
                RelayController(RelaySelection(6), true); //In this case, 6nd Relay will be opened
                timeMode = false;
            }
            else {
                RelayController(RelaySelection(6), false);
                timeMode = true;
            }
        }

        /// <summary>
        /// Sensors' Actions according to their limits
        /// </summary>
        /// <param name="sensorRead">Reading sensor value</param>
        /// <param name="sensorLimit">Getting sensor limit to compare</param>
        /// <returns>boolean to understand the situation</returns>
        static bool sensorAction(double sensorRead, int sensorLimit){
            bool timeOn = true;
            if (sensorRead >= sensorLimit){
                timeOn = false;
            }

            return timeOn;
        }

        /// <summary>
        /// Turn int dayOfWeek to String
        /// </summary>
        /// <param name="dayOfWeek"></param>
        /// <returns>String day of week</returns>
        static string DayOfWeekToString(int dayOfWeek)
        {
            string day = "";

            switch (dayOfWeek)
            {
                case 1:
                    day = "Sunday";
                    break;
                case 2:
                    day = "Monday";
                    break;
                case 3:
                    day = "Tuesday";
                    break;
                case 4:
                    day = "Wednesday";
                    break;
                case 5:
                    day = "Thursday";
                    break;
                case 6:
                    day = "Friday";
                    break;
                case 7:
                    day = "Saturday";
                    break;

            }
            //  Debug.Print("Day: " + day);

            return day;
        }

        /// <summary>
        /// Arrange Relay State(Open or Close)
        /// </summary>
        /// <param name="relayNo">OutputPort relay[relayNo]</param>
        /// <param name="relayState">True for opening, False for closing </param>
        static void RelayController(OutputPort relayNo, bool relayState)
        {
            if (relayState)
            {
                relayNo.Write(false);
            }
            else {
                relayNo.Write(true);
            }
        }

        /// <summary>
        /// Relay Selection
        /// </summary>
        /// <param name="relayNo">Relay No (1 to 8)</param>
        /// <returns>OutputPort relay[selection]</returns>
        static OutputPort RelaySelection(int relayNo)
        {
            switch (relayNo)
            {
                case 1:
                    return relay01;
                    break;
                case 2:
                    return relay02;
                    break;
                case 3:
                    return relay03;
                    break;
                case 4:
                    return relay04;
                    break;
                case 5:
                    return relay05;
                    break;
                case 6:
                    return relay06;
                    break;
                case 7:
                    return relay07;
                    break;
                case 8:
                    return relay08;
                    break;
                default:
                    return relay01;
                    break;
            }
        }
        /// <summary>
        /// Main Alarm System
        /// </summary>
        /// <param name="relayNo"></param>
        /// <param name="currentHour"></param>
        /// <param name="currentMinute"></param>
        /// <param name="currentDay"></param>
        /// <param name="hourAct">Relay opens: hr</param>
        /// <param name="minuteAct">Relay opens: min</param>
        /// <param name="hourReAct">Relay closes: hr</param>
        /// <param name="minuteReAct">Relay closes: min</param>
        /// <param name="daysAct">Relay opens at specific day or days of week</param>
        static void TimeToAct(int relayNo, int currentHour, int currentMinute, String currentDay, int hourAct, int minuteAct, int hourReAct, int minuteReAct, String[] daysAct)
        {
            for (int i = 0; i < 7; i++)
            {
                if (currentDay == daysAct[i])
                {
                    if (hourReAct == hourAct)
                    { //If starting hour and ending hour are in the same hour interval -such as 12.05 12.45
                        if (hourAct == currentHour)
                        { //If starting hour and current hour are in the same hour interval
                            if (minuteAct < currentMinute && currentMinute < minuteReAct)
                            {
                                Debug.Print((minuteReAct - currentMinute) + " minutes remaining to close the relay");
                                RelayController(RelaySelection(relayNo), true); //OPEN
                            }
                            else {
                                Debug.Print("CLOSED");
                                RelayController(RelaySelection(relayNo), false); //CLOSE
                            }
                        }
                        else {
                            Debug.Print("CLOSED HOUR");
                            RelayController(RelaySelection(relayNo), false); //CLOSE
                        }
                    }
                    else {
                        if (hourAct == currentHour)
                        { //If starting hour and current hour are in the same hour interval
                            if (minuteAct < currentMinute)
                            {
                                Debug.Print((hourReAct - currentHour) + " hours and " + (minuteReAct - currentMinute) +
                                    "minutes remaining to close the relay");
                                RelayController(RelaySelection(relayNo), true); //OPEN
                            }
                            else {
                                Debug.Print("CLOSED ACT HOUR");
                                RelayController(RelaySelection(relayNo), false); //CLOSE
                            }
                        }
                        else if (hourReAct == currentHour)
                        {
                            if (minuteReAct > currentMinute)
                            {
                                Debug.Print((hourReAct - currentHour) + " hours and " + (minuteReAct - currentMinute) +
                                      "minutes remaining to close the relay");
                                RelayController(RelaySelection(relayNo), true); //OPEN
                            }
                            else {
                                Debug.Print("CLOSED SAME REACT HOUR");
                                RelayController(RelaySelection(relayNo), false); //CLOSE
                            }
                        }
                        else if ((hourAct < currentHour) && (hourReAct > currentHour))
                        {
                            Debug.Print((hourReAct - currentHour) + " hours and " + (minuteReAct - currentMinute) +
                                      "minutes remaining to close the relay");
                            RelayController(RelaySelection(relayNo), true); //OPEN
                        }
                        else {
                            Debug.Print("CLOSED ROOT");
                            RelayController(RelaySelection(relayNo), false); //CLOSE
                        }

                    }

                }
            }

        }
        /// <summary>
        /// Detect Day depending on first 7 prime numbers
        /// 2 --> Monday
        /// 3 --> Tuesday
        /// 5 --> Wednesday
        /// 7 --> Thursday
        /// 11 --> Friday
        /// 13 --> Saturday
        /// 17 --> Sunday
        /// If we want our relay to open only one day, we need to enter days' prime number, for instance 5 for Wednesday.
        /// If we want our delay to open multiple days, we need to multiply days' prime numbers.
        /// For instance, all the days of week, we need to enter 2*3*5*7*11*13*17 = 510510
        /// Monday + Tuesday = 2*3 = 6
        /// Wednesday + Saturday + Sunday = 5*13*17 = 1105
        /// </summary>
        /// <param name="dayNumber">dayNumber</param>
        /// <returns>Detect Day String array</returns>
        static String[] DetectDay(int dayNumber)
        {
            String[] days = { "", "", "", "", "", "", "" };
            for (int i = 0; i < 7; i++)
            {
                days[i] = "";
            }
            if (dayNumber % 2 == 0) { days[0] = "Monday"; }
            if (dayNumber % 3 == 0) { days[1] = "Tuesday"; }
            if (dayNumber % 5 == 0) { days[2] = "Wednesday"; }
            if (dayNumber % 7 == 0) { days[3] = "Thursday"; }
            if (dayNumber % 11 == 0) { days[4] = "Friday"; }
            if (dayNumber % 13 == 0) { days[5] = "Saturday"; }
            if (dayNumber % 17 == 0) { days[6] = "Sunday"; }

            Debug.Print("DayNo: " + dayNumber);
            Debug.Print("Days: ");
            for (int i = 0; i < 7; i++)
            {
                if (days[i].Length > 5)
                {
                    Debug.Print("-" + days[i]);
                }
            }
            return days;
        }

    }
}
