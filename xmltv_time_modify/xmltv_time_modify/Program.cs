using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace xmltv_time_modify
{
  class Program
  {
    static String inputXml = "epg.xml";
    static String outputXml = "epg_times_modified.xml";
    static String configXml = "chans2correct.xml";

    static void Main(string[] args)
    {
      inputXml = (args.Length > 1) ? args[0] : inputXml;
      System.Console.WriteLine("Input EPG file: {0}", inputXml);

      outputXml = (args.Length > 2) ? args[1] : outputXml;
      System.Console.WriteLine("Output EPG file: {0}", outputXml);

      configXml = (args.Length > 3) ? args[2] : configXml;
      System.Console.WriteLine("Using config file: {0}", configXml);
      var chanenlsToCorrect = LoadChannelsToCorrect(configXml);
      Dictionary<String, int> channelsNotCorrected = chanenlsToCorrect;

      XDocument xmlFile = XDocument.Load(inputXml);
      var programmes = from c in xmlFile.Elements("tv").Elements("programme") select c;
      var offset = 0;
      var channel_id = String.Empty;
      var cOutput = "";

      foreach (XElement programme in programmes)
      {
        var channel_id_temp = programme.Attribute("channel").Value;
        if (chanenlsToCorrect.ContainsKey(channel_id_temp))
        {
          if (channel_id != channel_id_temp) // Is this a new channel?
          {
            if (channel_id != String.Empty)
            {
              Console.Write("\r{0}. Done!              \r\n", cOutput);
              channelsNotCorrected.Remove(channel_id);
            }
            channel_id = channel_id_temp;
            chanenlsToCorrect.TryGetValue(channel_id, out offset);
            var sOffset = offset.ToString("+#;-#;0"); ;
            cOutput = String.Format("Processing channel {0}, offset {1}", channel_id, sOffset);
            Console.Write(cOutput);
          }
                 
          var startTime = programme.Attribute("start").Value;
          TimeCorrect(ref startTime, offset);
          programme.Attribute("start").Value = startTime;

          var endTime = programme.Attribute("stop").Value;
          TimeCorrect(ref endTime, offset);
          programme.Attribute("stop").Value = endTime;
        }
      }
      Console.Write("\r{0}. Done!              \r\n", cOutput);
      xmlFile.Save(outputXml);
      Console.WriteLine("--------------------------------");
      Console.WriteLine("{0} channels were not corrected!", channelsNotCorrected.Count);
      foreach (var k in channelsNotCorrected.Keys)
        Console.WriteLine(k);

      //System.Console.ReadKey();
    }

    static void TimeCorrect(ref string dateTime, int correction)
    {
      try
      {
        var dateTimeSplit = dateTime.Split(' ');
        DateTime dDateTime = DateTime.ParseExact(dateTimeSplit[0], "yyyyMMddHHmmss", null);
        dDateTime = dDateTime.AddHours(Convert.ToDouble(correction));
        dateTime = dDateTime.ToString("yyyyMMddHHmmss") + " " + dateTimeSplit[1];
      }
      catch(Exception ex)
      {
        Console.WriteLine("Error {0}", ex.ToString());
      }
    }
    static Dictionary<String, int> LoadChannelsToCorrect(String configXml)
    {
      var d = new Dictionary<String, int>();
      if (File.Exists(configXml))
      {
        XmlDocument doc = new XmlDocument();
        doc.Load(configXml);
        XmlNode node = doc.SelectSingleNode("channels");
        foreach (XmlNode childNode in node.ChildNodes)
        {
          if (childNode.NodeType != XmlNodeType.Comment)
          {
            try
            {
              var channelName = childNode.InnerText;
              d.Add(channelName, Convert.ToInt16(childNode.Attributes["time_error"].Value));
              //Console.WriteLine("Correction for {0} {1}", channelName, childNode.Attributes["time_error"].Value);
            } 
            catch(Exception e)
            {
              Console.WriteLine(e.Message);
            }
          }
        }
      }
      else
      {
        System.Console.WriteLine("Configuration file {0} does not exist!", configXml);
      }
      return d;
    }
  }
}
