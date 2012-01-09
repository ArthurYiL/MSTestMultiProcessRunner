using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTestRunner
{
  class TestRunner
  {
    private readonly string _msTestExePath;
    private readonly string _resultPath;
    private readonly int _coreCount;
    private readonly string _runConfigPath;
    private List<string> _assemblyPaths;

    public TestRunner(string msTestPath, string resultPath, int coreCount, string runConfigPath)
    {
      _msTestExePath = msTestPath;
      _resultPath = resultPath;
      _coreCount = coreCount;
      _runConfigPath = runConfigPath;
      _assemblyPaths = new List<string>();
    }

    public void AddAssembly(string assemblyPath)
    {
      _assemblyPaths.Add(assemblyPath);
    }

    public static IEnumerable<string> GetAllNameSpacesThatHaveTestsInThem(Assembly testAssembly)
    {
      var allNameSpacesThatHaveTestsInThem = new List<string>();
      foreach (var type in testAssembly.GetTypes())
      {
        var members = type.GetMembers();
        if (members.Any() == true)
        {
          foreach (var member in members)
          {
            var check = member.GetCustomAttributes(typeof(TestMethodAttribute), false);
            if (check.Any() == true)
            {
              allNameSpacesThatHaveTestsInThem.Add(member.DeclaringType.Namespace);
            }
          }
        }
      }

      return allNameSpacesThatHaveTestsInThem;
    }

    public static IEnumerable<IEnumerable<TValue>> DivideEnumberableInToSubEnumberables<TValue>(IList<TValue> originalList, int divisor)
    {
      var returnList = new List<List<TValue>>();
      for (var i = 0; i < divisor; i++)
      {
        returnList.Add(new List<TValue>());
      }
      for (var i = 0; i < originalList.Count(); i++)
      {
        var remainder = i % divisor;
        returnList[remainder].Add(originalList[i]);
      }
      return returnList;
    }

    public void RunTests()
    {
      var allNameSpacesThatHaveTestsInThem = new List<string>();
      foreach (var assemblyPath in _assemblyPaths)
      {
        var testAssembly = Assembly.LoadFrom(assemblyPath);
        allNameSpacesThatHaveTestsInThem.AddRange(GetAllNameSpacesThatHaveTestsInThem(testAssembly));
      }
      
      var distinctNameSpacesThatHaveTestsInThem = allNameSpacesThatHaveTestsInThem.Distinct().ToList();

      IEnumerable<IEnumerable<string>> listOfTests;
      if (distinctNameSpacesThatHaveTestsInThem.Count == 1)
      {
        listOfTests = seperateSingleNameSpaceInToAlphabeticalSplit(distinctNameSpacesThatHaveTestsInThem[0], _coreCount);
      }
      else
      {
        listOfTests = DivideEnumberableInToSubEnumberables(distinctNameSpacesThatHaveTestsInThem, _coreCount);
      }

      ThreadPool.SetMaxThreads(_coreCount, _coreCount);
      if (Directory.Exists(_resultPath) == false)
      {
        Directory.CreateDirectory(_resultPath);
      }
      int i = 0;
      var events = new ManualResetEvent[_coreCount];
      foreach (var tests in listOfTests)
      {
        events[i] = new ManualResetEvent(false);
        RunTest(tests, i, events[i]);
        i++;
      }
      WaitHandle.WaitAll(events);
    }

    private IEnumerable<IEnumerable<string>> seperateSingleNameSpaceInToAlphabeticalSplit(string baseNameSpace, int coreCount)
    {
      var returnList = new List<List<string>>();
      var alphabet = new List<string>(){"a","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p","q","r","s","t","u","v","w","x","y","z","@","_"};
      var subAlphabets = DivideEnumberableInToSubEnumberables(alphabet, coreCount);
      foreach (var subAlphabet in subAlphabets)
      {
        var list = new List<string>();
        returnList.Add(list);
        foreach (var letter in subAlphabet)
        {
          list.Add(baseNameSpace + "." + letter);
        }
      }
      return returnList;
    }

    private void RunTest(IEnumerable<string> tests, int procNumber, ManualResetEvent manualEvent  )
    {
      var param = new object[4];
      var testNameSpacesToRun = string.Empty;
      foreach (var test in tests)
      {
        testNameSpacesToRun += "/test:" + test + " ";
      }
      param[0] = testNameSpacesToRun;
      param[1] = procNumber;
      param[2] = manualEvent;
      ThreadPool.QueueUserWorkItem(CallBack, param);
    }

    public void CallBack(object item)
    {
      var funcParamrs = (object[])item;
      var tests = (string)funcParamrs[0];
      var number = (int)funcParamrs[1];
      var manualEvent = (ManualResetEvent)funcParamrs[2];
      var p = new Process();
      //"C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\mstest.exe" /testcontainer:"C:\Path\To\Some\Test.dll" /test:SOME.NAME.SPACE.HERE
      var resultFile = string.Format("\"{0}\\{1}_{2}.result\"", _resultPath, number, GetFileNameComaptibleCurrentTime());
      var allTestDllPaths = string.Empty;
      foreach (var testDllPath in _assemblyPaths)
      {
        allTestDllPaths += "/testcontainer:\"" + testDllPath + "\" ";
      }
      var parameters = string.Format("{0} {1} /resultsfile:\"{2}\" /runconfig:\"{3}\"", allTestDllPaths, tests, resultFile, _runConfigPath);
      p.StartInfo = new ProcessStartInfo(_msTestExePath, parameters);
      p.Start();
      p.WaitForExit();
      manualEvent.Set();
    }

    private static string GetFileNameComaptibleCurrentTime()
    {
      var now = DateTime.Now;
      return string.Format("{0}_{1}_{2}_{3}_{4}", now.Month, now.Day, now.Year, now.Hour, now.Second);
    }
  }
}
