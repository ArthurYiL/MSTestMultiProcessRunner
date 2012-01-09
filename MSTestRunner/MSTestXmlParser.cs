using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace MSTestRunner
{
  public class MSTestXmlParser
  {

    public static bool DidAllTestsPass(string resultsFile)
    {
      var doc = new XmlDocument();
      doc.Load(resultsFile);
      var results = doc.GetElementsByTagName("Counters");
      var total = results[0].Attributes["total"];
      var passed = results[0].Attributes["passed"];
      if (total.Value != passed.Value)
      {
        return false;
      }
      return true;
    }

    public static void CombineAllResultFiles(string resultsFileFolderPath, string resultFilesExtension, string finalResultFilePath)
    {
      var filePaths = Directory.GetFiles(resultsFileFolderPath, "*." + resultFilesExtension);

      var finalDoc = new XmlDocument();
      var dec = finalDoc.CreateXmlDeclaration("1.0", null, null);
      finalDoc.AppendChild(dec);

      var root = finalDoc.CreateElement("TestRun");
      finalDoc.AppendChild(root);

      var allTestDefs = finalDoc.CreateElement("TestDefinitions");
      root.AppendChild(allTestDefs);

      var allTestLists = finalDoc.CreateElement("TestLists");
      root.AppendChild(allTestLists);

      var allTestEntries = finalDoc.CreateElement("TestEntries");
      root.AppendChild(allTestEntries);

      var allResults = finalDoc.CreateElement("Results");
      root.AppendChild(allResults);

      var summaryDict = new Dictionary<string, int>();

      var isFirst = true;

      foreach (var filePath in filePaths)
      {
        var doc = new XmlDocument();
        doc.Load(filePath);

        if (isFirst == true)
        {
          var names = new List<string>() { "ResultSummary", "Times", "TestSettings" };
          foreach (var name in names)
          {
            var element = doc.GetElementsByTagName(name)[0];
            var importElement = root.OwnerDocument.ImportNode(element, true);
            root.PrependChild(importElement);
          }
          isFirst = false;
        }

        var counters = doc.GetElementsByTagName("Counters")[0];
        foreach (XmlAttribute attribute in counters.Attributes)
        {
          var value = Int32.Parse(attribute.Value);
          if (summaryDict.ContainsKey(attribute.Name))
          {
            summaryDict[attribute.Name] += value;
          }
          else
          {
            summaryDict.Add(attribute.Name, value);
          }
        }

        var testDefs = doc.GetElementsByTagName("TestDefinitions")[0];
        AppendNodesToElement(allTestDefs, testDefs.ChildNodes);
        var testLists = doc.GetElementsByTagName("TestLists")[0];
        AppendNodesToElement(allTestLists, testLists.ChildNodes);
        var testEntries = doc.GetElementsByTagName("TestEntries")[0];
        AppendNodesToElement(allTestEntries, testEntries.ChildNodes);
        var testResults = doc.GetElementsByTagName("Results")[0];
        AppendNodesToElement(allResults, testResults.ChildNodes);
      }

      var finalCounters = finalDoc.GetElementsByTagName("Counters")[0];
      foreach (var item in summaryDict)
      {
        var test = finalCounters.Attributes[item.Key];
        test.Value = item.Value.ToString();
      }

      var settings = new XmlWriterSettings();
      settings.Indent = true;
      var outFile = XmlWriter.Create(finalResultFilePath, settings);
      finalDoc.WriteTo(outFile);
      outFile.Close();
    }

    private static void AppendNodesToElement(XmlElement element, XmlNodeList nodeList)
    {
      foreach (XmlNode node in nodeList)
      {
        var importNode = element.OwnerDocument.ImportNode(node, true);
        element.AppendChild(importNode);
      }
    }
  }
}
