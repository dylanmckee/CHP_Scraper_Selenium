using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using static System.Net.Mime.MediaTypeNames;

namespace CHP_Selenium_Scraper
{
    class Program
    {
        static void Main(string[] args)
        {
            //IWebDriver driver;
            //driver = new ChromeDriver("C:/Drivers/chromedriver.exe");
            //driver.Url = "https://cad.chp.ca.gov/traffic.aspx?__EVENTTARGET";
            //driver.Manage().Window.Maximize();
            Scraper scraper = new Scraper();
            scraper.intialize();



        }

    }
}
