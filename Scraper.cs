using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using System.Timers;
using System.Net.Http;

namespace CHP_Selenium_Scraper
{
    class Scraper
    {
        System.Timers.Timer t1;
        //System.Windows.Forms.Timer t1;
        WebDriver driver;
        Boolean stopped = false;
        //  System.Windows.Forms.Timer t;
        int incidentCount = 0;
        public Scraper()
        {

        }

        public void intialize()
        {
            this.tb = tb;
            //Do intiial scraping run before timer starts
            runScraper();
            //Run timer
            startTimer();
            //Console.WriteLine("\nPress the Enter key to exit the application...\n");
            //Console.WriteLine("The application started at {0:HH:mm:ss.fff}", DateTime.Now);
            //Console.ReadLine();
            //t.Stop();
            //t.Dispose();

        }

        private async void runScraper()
        {
            incidentCount = 0;
            // create driver and open page
            //driver = new ChromeDriver("C:/Drivers/chromedriver.exe");
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("headless");
            driver = new ChromeDriver(chromeOptions);
            driver.Url = "https://cad.chp.ca.gov/traffic.aspx?__EVENTTARGET";
            // check 'auto refresh off' button
            IWebElement autoRefresh = driver.FindElement(By.Id("chkAutoRefresh"));
            autoRefresh.Click();
            // get list of all incidents
            List<List<string>> resultList = returnAllCommCenters();

            string jsonString = convertToJson(resultList);
            await sendToBackendAsync(jsonString);
            driver.Close();
            driver.Quit();


            //foreach (List<string> col in resultList)
            //{
            //    foreach (string str in col)
            //    {
            //        Console.Write(str + "  ");
            //    }
            //    Console.WriteLine();
            //}

        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            // run web scraper at timer interval
            Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}",
              e.SignalTime);

            runScraper();
        }
        private List<List<string>> returnAllCommCenters()
        {
            List<string> commCenters = new List<string>{ "Bakersfield","Barstow","Bishop","Border","Capitol","Chico", "El Centro",
                 "Fresno", "Golden Gate", "Humboldt", "Indio", "Inland", "Los Angeles", "Merced", "Monterey", "Orange",
                 "Redding", "Sacramento", "San Luis Obispo", "Stockton", "Susanville", "Truckee", "Ukiah", "Ventura", "Yreka"};
            List<List<string>> result = new List<List<string>>();
            // run scraper for each comm center
            foreach (string cc in commCenters)
            {
                //Console.WriteLine("COMM CENTER:  " + cc);

                SelectElement dropDown = new SelectElement(driver.FindElement(By.Id("ddlComCenter")));
                dropDown.SelectByText(cc);
                List<List<string>> areaResult = getTableInfo(cc);
                // check for null -- empty table w/ no incidents
                if (areaResult != null)
                {
                    // append incidents from each comm center
                    foreach (List<string> col in areaResult)
                    {
                        col.Add(cc);
                        result.Add(col);
                    }
                }
            }
            incidentCount = result.Count();
            // Print finished list 
            //foreach (List<string> sl in result)
            //{
            //    foreach (string str in sl)
            //    {
            //        Console.Write(str + "   ");
            //    }
            //    Console.WriteLine();
            //}
            return result;


        }
        private List<List<string>> getTableInfo(string cc)
        {
            // get details links
            int numIncidents = 0;
            IList<IWebElement> details;
            details = driver.FindElements(By.LinkText("Details"));
            // Console.WriteLine("Details ilist count : " + details.Count);
            numIncidents = details.Count;


            // create list<list> in string format
            List<List<string>> stringList = new List<List<string>>();
            List<IList<IWebElement>> fullList = new List<IList<IWebElement>>();

            for (int i = 0; i < details.Count; i++)
            {
                // get all 'details' links
                details = driver.FindElements(By.LinkText("Details"));
                details[i].Click();

                IWebElement t;
                IList<IWebElement> rowList;
                try
                {
                    // get table
                    t = driver.FindElement(By.Id("gvIncidents"));
                    // get list of rows from table
                    rowList = t.FindElements(By.TagName("tr"));
                }
                catch (Exception e)
                {
                    Console.WriteLine("No data" + e.ToString());
                    return null;
                }


                // get element of individual incident and separate by column
                IWebElement currRow = rowList[i + 1];
                IList<IWebElement> columns = currRow.FindElements(By.TagName("td"));
                List<string> resultList = new List<string>();
                foreach (IWebElement col in columns)
                {
                    // get strings for each column
                    resultList.Add(col.Text);
                }

                IWebElement latlon = driver.FindElement(By.Id("lblLatLon"));
                IWebElement detailsNode = driver.FindElement(By.Id("tblDetails"));
                // Add comm center, lat/lon, and details information from bottom
                resultList.Add(cc);
                resultList.Add(latlon.Text);
                IList<IWebElement> latlonLinks = latlon.FindElements(By.TagName("a"));
                string mapsURL = latlonLinks[0].GetAttribute("href");
                resultList.Add(mapsURL);
                string detailsString = "";
                // split up table by row-- put in array
                foreach (IWebElement row in detailsNode.FindElements(By.TagName("tr")))
                {
                    detailsString += row.Text + "\n";
                }
                string s = detailsNode.Text;

                //s.Replace("\n", "\\n");
                resultList.Add(detailsString);

                stringList.Add(resultList);

            }




            return stringList;
        }
        private string convertToJson(List<List<string>> res)
        {
            //foreach (List<string> l in res) {
            //    l[10].Replace("\n", "\\");
            //}
            string result = "";
            result = result + "{\n";
            result = result + "\t\"Source\" : \"CHP\",\n";
            result = result + "\t \"Incidents\":\n\t[";
            for (int i = 0; i < res.Count; i++)
            {
                result = result + "\n\t\t{\"ID\": \"" + res[i][1] + "\",";
                result = result + "\n\t\t\"Time\": \"" + res[i][2] + "\",";
                result = result + "\n\t\t\"Type\": \"" + res[i][3] + "\",";
                result = result + "\n\t\t\"Location\": \"" + res[i][4] + "\",";
                result = result + "\n\t\t\"LocationDesc\": \"" + res[i][5] + "\",";
                result = result + "\n\t\t\"Area\": \"" + res[i][6] + "\",";
                result = result + "\n\t\t\"CommCenter\": \"" + res[i][7] + "\",";
                result = result + "\n\t\t\"LatLon\": \"" + res[i][8] + "\",";
                result = result + "\n\t\t\"LatLonURL\": \"" + res[i][9] + "\",";
                result = result + "\n\t\t\"Details\": [";
                //split up details table into each row-- remove newlines
                string[] detArr = res[i][10].Split('\n');
                for (int j = 0; j < detArr.Length - 1; j++)
                {
                    string cleaned = detArr[j].Replace("\n", "").Replace("/r", "");
                    cleaned = cleaned.TrimEnd('\\');
                    //if (cleaned.Contains('\n') || cleaned.Contains('\r'))
                    //    Console.WriteLine("Found a newline");
                    result = result + "\n\t\t\t\"" + cleaned + "\"";
                    if (j != detArr.Length - 2)
                        result = result + ",";
                }

                result = result + "\n\t\t\t]\n\t\t}";

                if (i != res.Count - 1)
                    result = result + ",";

                result = result + "\n";
            }
            result = result + "\t]\n}";
            Console.WriteLine(result);

            return result;
        }

        private async Task sendToBackendAsync(string jsonString)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    Console.WriteLine("Sending to transformer");
                    //var response = await client.PostAsync(

                    //   "https://polished-meadow-5286.fly.dev/api/chp",
                    //    new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    Console.WriteLine("Sent to transformer");
                    //tb.Text += "Sent " + incidentCount + " incidents to transformer";
                    string txt = "Sent " + incidentCount + " incidents to transformer\n";
                }
                catch (Exception e)
                {
                    string txt_error = "Couldn't send data to transformer\n";
                    tb.Text = tb.Text.Insert(0, txt_error);
                    Console.WriteLine("Couldn't send data to transformer\n" + e.Message);
                }
            }
        }

        private void startTimer()
        {
            t1 = new System.Timers.Timer();

            t1.Start();
            //t.Elapsed += OnTimedEvent;

            //t.AutoReset = true;
            //t.Enabled = true;
        }
        private void t1_Tick(object sender, EventArgs e)
        {
            // run web scraper at timer interval
            //Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}",
            //  );
            //tb.Clear();
            if (!stopped)
                runScraper();
        }
        public void endScraper()
        {
            stopped = true;
            driver.Quit();
            t1.Stop();
        }
    }
}
