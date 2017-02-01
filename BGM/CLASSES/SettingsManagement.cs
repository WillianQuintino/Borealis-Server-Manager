﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace GameServer_Manager
{
    public class SettingsManagement_Classes
    {

        //=====================================================================================//
        // Read & Write XML Data Functions                                                     //
        // https://www.codeproject.com/Articles/30834/Storing-and-Retrieving-Settings-from-XML //
        //=====================================================================================//
        public static void CheckForConfigXML()
        {
            if (System.IO.File.Exists(Environment.CurrentDirectory + @"\config.xml"))
            {
                //Dont do anything!
            }
            else
            {
                //Generate a config file!
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;

                XmlWriter writer = XmlWriter.Create("config.xml", settings);
                writer.WriteStartDocument();
                writer.WriteComment("This file is generated by GSM.  Feel free to modify it if you know what you are doing.");

                writer.WriteStartElement("config");

                //Generate Global Settings
                writer.WriteComment("GLOBAL SETTINGS");
                writer.WriteStartElement("global_settings");
                writer.WriteElementString("operator_name", "Operator");
                writer.WriteElementString("gameserver_storage_directory", @"C:\GSM\");
                writer.WriteEndElement(); //End global_settings

                //Generate Gameserver Listing
                writer.WriteComment("GAMESERVER CONFIGURATION LISTING");
                writer.WriteStartElement("gameserver_listing");
                /**/
                writer.WriteStartElement("server");
                writer.WriteAttributeString("ID", "001");
                writer.WriteElementString("server_alias", "Phantom Insanity");
                writer.WriteElementString("server_type", "Garry's Mod");
                writer.WriteElementString("running_status", "Offline");
                writer.WriteElementString("install_location", @"C:\GSM\steamapps\common\GarrysModDS");
                writer.WriteEndElement();

                writer.WriteStartElement("server");
                writer.WriteAttributeString("ID", "002");
                writer.WriteElementString("server_alias", "Death Row");
                writer.WriteElementString("server_type", "Killing Floor 2");
                writer.WriteElementString("running_status", "Offline");
                writer.WriteElementString("install_location", @"C:\GSM\steamapps\common\killingfloor2");
                writer.WriteEndElement();

                writer.WriteEndElement(); //End gameserver_listing

                //writer.WriteAttributeString("Name", "Phantom Cthulu Hole");
                //writer.WriteElementString("Price", "10.00");

                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();

            }
        }

        public static string ReadValueFromXML(string settingsFilePath, string pstrValueToRead)
        {
            try
            {
                //settingsFilePath is a string variable storing the path of the settings file 
                XPathDocument doc = new XPathDocument(settingsFilePath);
                XPathNavigator nav = doc.CreateNavigator();
                // Compile a standard XPath expression
                XPathExpression expr;
                expr = nav.Compile(@"/settings/" + pstrValueToRead);
                XPathNodeIterator iterator = nav.Select(expr);
                // Iterate on the node set
                while (iterator.MoveNext())
                {
                    return iterator.Current.Value;
                }
                return string.Empty;
            }
            catch
            {
                //do some error logging here. Leaving for you to do 
                return string.Empty;
            }
        }

        public static bool WriteValueTOXML(string settingsFilePath, string pstrValueToRead, string pstrValueToWrite)
        {
            try
            {
                //settingsFilePath is a string variable storing the path of the settings file 
                XmlTextReader reader = new XmlTextReader(settingsFilePath);
                XmlDocument doc = new XmlDocument();
                doc.Load(reader);
                //we have loaded the XML, so it's time to close the reader.
                reader.Close();
                XmlNode oldNode;
                XmlElement root = doc.DocumentElement;
                oldNode = root.SelectSingleNode("/settings/" + pstrValueToRead);
                oldNode.InnerText = pstrValueToWrite;
                doc.Save(settingsFilePath);
                return true;
            }
            catch
            {
                //properly you need to log the exception here. But as this is just an
                //example, I am not doing that. 
                return false;
            }
        }

        public static string GameServerXMLData(string serverName, string elementToFind)
        {
            //Read the XML file to determine what the launch parameters are.
            var xdoc = XDocument.Load(Environment.CurrentDirectory + @"\gameservers_data.xml");
            string retrievedValue = xdoc.Descendants("server")
                .Where(s => (string)s.Element("server_name") == serverName)
                .Select(s => (string)s.Element(elementToFind))
                .FirstOrDefault();
            return retrievedValue;
        }
    }
}
