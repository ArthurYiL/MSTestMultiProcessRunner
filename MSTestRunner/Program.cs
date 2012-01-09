using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml;

namespace MSTestRunner
{
  class Program
  {
    private static string _msTestPath;
    private static List<string> _testDllPaths;
    private static int _coreCount = 0;
    private static string _resultFolder;
    private static string FinalResultFile { get { return _resultFolder + "\\" + _finalResultFileName; } }
    private static string _finalResultFileName;
    private static string _runConfigPath;
    private static bool _shouldCombineResultsFile = true;

    //private static StreamWriter logFile;

    static void Main(string[] args)
    {
      _testDllPaths = new List<string>();
      //logFile = File.CreateText("C:\\LogFile.txt");
      //logFile.AutoFlush = true;
      //Console.WriteLine(Process.GetCurrentProcess().StartInfo.Arguments);
      //logFile.WriteLine(Process.GetCurrentProcess().StartInfo.Arguments);
      if (ParseArgs(args) == false)
      {
        return;
      }
      SetArgsIfNotSupplied();
      if (CheckIfFilesExist() == false)
      {
        return;
      }
      ClearPreviousResultFiles();
      var testRunner = new TestRunner(_msTestPath, _resultFolder, _coreCount, _runConfigPath);
      foreach (var dllPath in _testDllPaths)
      {
        testRunner.AddAssembly(dllPath);
      }
      var start = DateTime.Now;
      testRunner.RunTests();
      var end = DateTime.Now;
      var totalTime = end - start;
      Console.WriteLine(totalTime);
      if (_shouldCombineResultsFile == true)
      {
        MSTestXmlParser.CombineAllResultFiles(_resultFolder, "result", FinalResultFile);
        if (MSTestXmlParser.DidAllTestsPass(FinalResultFile) == false)
        {
          Console.WriteLine("Not All Tests Passed check file: \"{0}\" to see which ones failed", FinalResultFile);
        }
      }
    }

    private static void ClearPreviousResultFiles()
    {
      var filePaths = Directory.GetFiles(_resultFolder, "*.result");
      foreach (var filePath in filePaths)
      {
        File.Delete(filePath);
      }
    }

    private static bool CheckIfFilesExist()
    {
      var returnValue = true;
      var filesToCheck = new List<string> {_msTestPath};
      filesToCheck.AddRange(_testDllPaths);
      foreach (var filePath in filesToCheck)
      {
        if (File.Exists(filePath) == false)
        {
          Console.WriteLine("File {0} doesn't exist", filePath);
          returnValue = false;
        }
      }
      return returnValue;
    }

    private static void SetArgsIfNotSupplied()
    {
      if (_testDllPaths.Any() == false)
      {
        //_testDllPaths.Add(@"C:\Add\Your\Path\Here\Test.dll");
      }
      if (_coreCount == 0)
      {
        _coreCount = Environment.ProcessorCount;
      }
      if (_msTestPath == null)
      {
        _msTestPath = @"C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\mstest.exe";
      }
      if (_resultFolder == null)
      {
        _resultFolder = @"C:\Results\";
      }
      if (_finalResultFileName == null)
      {
        _finalResultFileName = "finalResult.xml";
      }
      if(_runConfigPath == null)
      {
        //_runConfigPath = @"C:\Add\Your\Path\Here\TestRunConfig.dll";
      }
      
    }

    private static bool ParseArgs(string[] args)
    {
      var returnValue = true;
      for(var i = 0; i < args.Count(); i++)
      {
        //logFile.WriteLine(args[i]);
        switch(args[i].ToUpper())
        {
          case "/TESTDLL":
            {
              _testDllPaths.Add(args[i + 1]);
              break;
            }
          case "/MSTESTEXE":
            {
              _msTestPath = args[i + 1];
              break;
            }
          case "/CORECOUNT":
            {
              _coreCount = Int32.Parse(args[i + 1]);
              break;
            }
          case "/RESULTLOC":
            {
              _resultFolder = args[i + 1];
              break;
            }
          case "/FINALRESULTFILE":
            {
              _finalResultFileName = args[i + 1];
              break;
            }
          case "/RUNCONFIG":
            {
              _runConfigPath = args[i + 1];
              break;
            }
          case "/NOAGGTESTRESULTS":
            {
              _shouldCombineResultsFile = false;
              break;
            }
          case "/HELP":
            {
              WriteHelpToConsole();
              returnValue = false;
              break;
            }
          case "/?":
            {
              WriteHelpToConsole();
              returnValue = false;
              break;
            }
        }
      }
      return returnValue;
    }

    private static void WriteHelpToConsole()
    {
      Console.WriteLine("/testdll Path to Test DLL\n" +
                                "/mstestexe path to MS Test\n" +
                                "/corecount number of instances to run concurrently\n" +
                                "/resultloc Directory to place result files\n\n" +
                                "Example: RPTestRunner.exe /testdll C:\\svn\\rps\\tests\\MyTestDll.dll /mstestexe C:\\Program Files\\VS\\MSTest.exe /corecount 2 /resultloc C:\\Results\n\n\n" +
                                "Defaults:\n" +
                                @"testdll: C:\svn\rps\Common\Tests\DomainModelTest\bin\Debug\DomainModelTest.dll" + "\n" +
                                @"mstest: C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\mstest.exe" + "\n" +
                                "corecount: The number of logical processors on your box\n" +
                                "resultloc: C:\\Results");
    }
  }
}
