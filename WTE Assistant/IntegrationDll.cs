﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WTE_Assistant
{
    public class IntegrationDll
    {
        public string DllName { get; set; }

        public string DllPath { get; set; }

        public int TotalTestNum { get; set; }

        public int PassedTestNum { get; set; }

        public int NotExecutedTestNum { get; set; }

        public int FailedTestNum { get; set; }

        public List<Test> Tests;

        public List<Test> FailedTests;
    }

    public class Test
    {
        public string TestName { get; set; }

        public string TestID { get; set; }

        public string ErrorMessage { get; set; }

        public string ErrorStackTrace { get; set; }

        public string DllName { get; set; }

        public TestResult Result { get; set; }
    }

    public enum TestResult
    {
        Passed,
        Failed,
        NotExecuted
    }
}