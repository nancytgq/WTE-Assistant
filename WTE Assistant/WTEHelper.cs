﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace WTE_Assistant
{
    public class WTEHelper
    {
        public static string HOMEDRIVE = System.Environment.GetEnvironmentVariable("HOMEDRIVE");
        public static string WOR = System.Environment.GetEnvironmentVariable("WOR");
        public static string IntegrationTestsDir = HOMEDRIVE + "\\TestBin\\IntegrationTests\\";
        public static string TestResultsDir = IntegrationTestsDir + "\\TestResults\\";
        public static string VSTestConsoleEnd = @"\Common7\IDE\CommonExtensions\Microsoft\TestWindow\VSTest.Console.exe";

        public string VSLocation = null;
        public int MaxResetTime = 1;

        public WTEHelper(int maxResetTime, string vsLocation)
        {
            //set the reset time
            this.MaxResetTime = maxResetTime;
            this.VSLocation = vsLocation;
        }
        
        public void Start()
        {
            List<IntegrationDllResult> IntegrationDllResults = GetIntegrationDllResults(GetDllResultFileList());
            ResetFailedTests(IntegrationDllResults);
            ShowResults(IntegrationDllResults);
        }

        /// <summary>
        /// Reset all failed tests
        /// </summary>
        /// <param name="IntegrationDllResults"></param>
        public void ResetFailedTests(List<IntegrationDllResult> IntegrationDllResults)
        {
            foreach(IntegrationDllResult IntegrationDllResult in IntegrationDllResults)
            {
               foreach(TestResult test in IntegrationDllResult.FailedTestResults)
                {
                    //1. 初始化reset命令
                    StringBuilder resetCommand = new StringBuilder();
                    resetCommand.Append("\"" + VSLocation + VSTestConsoleEnd + "\"");
                    resetCommand.Append(" /TestCaseFilter:Name~" + test.TestName);
                    resetCommand.Append(" \"" + IntegrationTestsDir + test.DllName + "\"");
                    resetCommand.Append(" /logger:trx");

                    //2. reset case，并收集结果
                    //3. 判断结果是passed还是failed，如果是passed，则将该case从failedTests中移除，并且passednum+1， failednum-1，然后继续reset下一条case
                    //4. 如果结果仍是failed，根据rest次数，再去reset case. 如果超出reset次数，那么继续reset下一条case
                    int resetTime = 1;
                    while(resetTime <= MaxResetTime)
                    {
                        ResetFailedTest(test);
                        UpdateTestResult(test);
                        resetTime++;

                        if (test.Outcome.Equals("Passed"))
                        {
                            IntegrationDllResult.FailedTestNum--;
                            IntegrationDllResult.PassedTestNum++;

                            //TODO: 继续reset下一条case
                        }
                    }                         
                }
            }

            //TODO: 输出结果到UI （Html或者WPF界面）
        }

        /// <summary>
        /// Reset Failed Test
        /// </summary>
        /// <param name="test"></param>
        public void ResetFailedTest(TestResult test)
        {
            //TODO: Implementation
        }

        /// <summary>
        /// Read the new trx file and update the test result
        /// </summary>
        /// <param name="test"></param>
        public void UpdateTestResult(TestResult test)
        {
            //TODO: Implementation
        }

        /// <summary>
        /// Show all results on UI
        /// </summary>
        /// <param name="IntegrationDllResults"></param>
        public void ShowResults(List<IntegrationDllResult> IntegrationDllResults)
        {
            //TODO: Implementation
        }

        /// <summary>
        /// Get all dll trx path
        /// </summary>
        /// <returns>trx path list</returns>
        public List<FileInfo> GetDllResultFileList()
        {
            List<FileInfo> DllResultFileList = new List<FileInfo>();

            //var files = Directory.GetFiles(TestResultsDir, "*.trx");
            DirectoryInfo TestResultsDirInfo = new DirectoryInfo(TestResultsDir);
            FileInfo[] filesInfo = TestResultsDirInfo.GetFiles("*.trx");

            //int index = 1;
            foreach (var file in filesInfo)
            {
                DllResultFileList.Add(file);
                //Console.WriteLine("{0}: {1}", index++, file);
            }

            return DllResultFileList;
        }

        /// <summary>
        /// Get all result informations of all Dll
        /// </summary>
        /// <param name="DllResultFileList"></param>
        /// <returns></returns>
        public List<IntegrationDllResult> GetIntegrationDllResults(List<FileInfo> DllResultFileList)
        {
            List<IntegrationDllResult> IntegrationDllResults = new List<IntegrationDllResult>();

            foreach (FileInfo DllResultFile in DllResultFileList)
            {
                IntegrationDllResults.Add(GetIntegrationDllResult(DllResultFile));
            }

            return IntegrationDllResults;
        }

        /// <summary>
        /// Get result informations of one dll
        /// </summary>
        /// <param name="DllResultFile"></param>
        /// <returns></returns>
        public IntegrationDllResult GetIntegrationDllResult(FileInfo DllResultFile)
        {
            IntegrationDllResult IntegrationDllResult = new IntegrationDllResult();

            try
            {
                XNamespace ns = @"http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
                var doc = XDocument.Load(DllResultFile.FullName);

                var testDefinitions = (from unitTest in doc.Descendants(ns + "UnitTest")
                                       select new
                                       {
                                           executionId = unitTest.Element(ns + "Execution").Attribute("id").Value,
                                           codeBase = unitTest.Element(ns + "TestMethod").Attribute("codeBase").Value,
                                           className = unitTest.Element(ns + "TestMethod").Attribute("className").Value,
                                           testName = unitTest.Element(ns + "TestMethod").Attribute("name").Value
                                       }
                             ).ToList();

                string[] sArray = testDefinitions[0].codeBase.Split('\\');
                IntegrationDllResult.DllName = sArray[sArray.Length - 1];

                var results = (from utr in doc.Descendants(ns + "UnitTestResult")
                               let executionId = utr.Attribute("executionId").Value
                               let message = utr.Descendants(ns + "Message").FirstOrDefault()
                               let stackTrace = utr.Descendants(ns + "StackTrace").FirstOrDefault()
                               let st = DateTime.Parse(utr.Attribute("startTime").Value).ToUniversalTime()
                               let et = DateTime.Parse(utr.Attribute("endTime").Value).ToUniversalTime()
                               select new TestResult()
                               {
                                   AssemblyPathName = (from td in testDefinitions where td.executionId == executionId select td.codeBase).Single(),
                                   FullClassName = (from td in testDefinitions where td.executionId == executionId select td.className).Single(),
                                   Outcome = utr.Attribute("outcome").Value,
                                   TestName = utr.Attribute("testName").Value,
                                   ErrorMessage = message == null ? "" : message.Value,
                                   StackTrace = stackTrace == null ? "" : stackTrace.Value,
                                   DllName = IntegrationDllResult.DllName,
                                   TestID = executionId
                               }
                               ).OrderBy(r => r.Outcome).
                                 ThenBy(r => r.TestName).
                                 ThenBy(r => r.FullClassName);

                IntegrationDllResult.TestResults = results.ToList();
                IntegrationDllResult.FailedTestResults = IntegrationDllResult.TestResults.Where(n => n.Outcome.Equals("Failed")).ToList();
                IntegrationDllResult.DllName = sArray[sArray.Length - 1];

                var counters = doc.Descendants(ns + "ResultSummary").FirstOrDefault().Descendants(ns + "Counters").FirstOrDefault();
                IntegrationDllResult.TotalTestNum = Int32.Parse(counters.Attribute("total").Value);
                IntegrationDllResult.PassedTestNum = Int32.Parse(counters.Attribute("passed").Value);
                IntegrationDllResult.FailedTestNum = Int32.Parse(counters.Attribute("failed").Value);

            }
            catch (Exception ex)
            {
                throw new Exception("Error while parsing Trx file '" + DllResultFile.FullName + "'\nException: " + ex.ToString());
            }

            return IntegrationDllResult;
        }

    }
}