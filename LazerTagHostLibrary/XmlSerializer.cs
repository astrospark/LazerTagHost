using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

namespace LazerTagHostLibrary
{
	public class XmlSerializer
	{
		public static Version VerifyFileInfo(XmlDocument xmlDocument, string expectedFileType, Version expectedFileTypeVersion)
		{
			var fileInfoNode = xmlDocument.SelectSingleNode("/LazerSwarm/FileInfo");
			if (fileInfoNode == null) throw new NullReferenceException("fileInfoNode");
			if (fileInfoNode.Attributes == null) throw new NullReferenceException("fileInfoNode.Attributes");
			var fileInfoVersionAttribute = fileInfoNode.Attributes["Version"];
			if (fileInfoVersionAttribute == null) throw new NullReferenceException("versionAttribute");
			var fileInfoVersion = new Version(fileInfoVersionAttribute.Value);
			if (fileInfoVersion > new Version(1, 0))
			{
				throw new FileFormatException(String.Format("Unsupported FileInfo version ({0}).", fileInfoVersion));
			}

			var fileTypeNode = fileInfoNode.SelectSingleNode("./FileType");
			if (fileTypeNode == null) throw new NullReferenceException("fileTypeNode");
			var fileType = fileTypeNode.InnerText;
			if (fileType != expectedFileType)
			{
				throw new FileFormatException(String.Format("Incorrect FileType, \"{0}\". Expected, \"{1}\".", fileType,
				                                            expectedFileType));
			}

			var fileTypeVersionNode = fileInfoNode.SelectSingleNode("./FileTypeVersion");
			if (fileTypeVersionNode == null) throw new NullReferenceException("fileTypeVersionNode");
			var fileTypeVersion = new Version(fileTypeVersionNode.InnerText);
			if (fileTypeVersion > expectedFileTypeVersion)
			{
				throw new FileFormatException(String.Format("Unsupported FileTypeVersion, \"{0}\". Expected, \"{1}\".",
				                                            fileInfoVersion, expectedFileTypeVersion));
			}

			return fileTypeVersion;
		}

		public static void WriteDocumentStart(XmlTextWriter xmlTextWriter)
		{
			xmlTextWriter.WriteStartDocument();
			xmlTextWriter.WriteStartElement("LazerSwarm");
		}

		public static void WriteDocumentEnd(XmlTextWriter xmlTextWriter)
		{
			xmlTextWriter.WriteEndElement();
		}

		public static void WriteFileInfo(XmlTextWriter xmlTextWriter, string fileType, Version fileTypeVersion)
		{
			xmlTextWriter.WriteStartElement("FileInfo");
			xmlTextWriter.WriteAttributeString("Version", "1.0");
			xmlTextWriter.WriteElementString("FileType", fileType);
			xmlTextWriter.WriteElementString("FileTypeVersion", fileTypeVersion.ToString(2));
			xmlTextWriter.WriteEndElement();
		}

		public static void WriteNodeText(XmlWriter xmlWriter, string nodeName, string text)
		{
			xmlWriter.WriteStartElement(nodeName);
			xmlWriter.WriteString(text);
			xmlWriter.WriteEndElement();
		}

		public static void WriteNodeTextBool(XmlWriter xmlWriter, string nodeName, bool value)
		{
			WriteNodeText(xmlWriter, nodeName, value.ToString());
		}

		public static void WriteNodeTextInt(XmlWriter xmlWriter, string nodeName, int value)
		{
			var text = value == 0xff ? "0xff" : value.ToString(CultureInfo.InvariantCulture);
			WriteNodeText(xmlWriter, nodeName, text);
		}

		public static string GetNodeText(XmlNode parentNode, string nodeName)
		{
			var node = parentNode.SelectSingleNode(String.Format("./{0}", nodeName));
			if (node == null) throw new NullReferenceException("node");
			return node.InnerText;
		}

		public static string GetNodeLocalizedText(XmlNode parentNode, string nodeName, CultureInfo cultureInfo = null)
		{
			var node = parentNode.SelectSingleNode(String.Format("./{0}", nodeName));
			if (node == null) throw new NullReferenceException("node");

			var localizedTextNode = SelectLocalizedTextNode(node, cultureInfo);
			if (localizedTextNode == null) throw new NullReferenceException("localizedTextNode");

			return localizedTextNode.InnerText;
		}

		public static int GetNodeTextInt(XmlNode parentNode, string nodeName)
		{
			return StringToInt(GetNodeText(parentNode, nodeName));
		}

		public static bool GetNodeTextBool(XmlNode parentNode, string nodeName)
		{
			return Convert.ToBoolean(GetNodeText(parentNode, nodeName));
		}

		public static XmlNode SelectLocalizedTextNode(XmlNode parentNode, CultureInfo cultureInfo = null)
		{
			XmlNode localizedTextNode = null;
			if (cultureInfo != null)
			{
				localizedTextNode = parentNode.SelectSingleNode(String.Format("./LocalizedText[@Culture='{0}']",
				                                                              cultureInfo.Name));
			}

			// Fall back to CurrentUICulture
			if (localizedTextNode == null)
			{
				localizedTextNode = parentNode.SelectSingleNode(String.Format("./LocalizedText[@Culture='{0}']",
				                                                              Thread.CurrentThread.CurrentUICulture.Name));
			}

			// Fall back to first or only LocalizedText node
			if (localizedTextNode == null)
			{
				localizedTextNode = parentNode.SelectSingleNode("./LocalizedText");
			}

			return localizedTextNode;
		}

		public static int StringToInt(string input)
		{
			var regex = new Regex(@"^\s*0x([0-9a-fA-F]{1,8})\s*$");
			var match = regex.Match(input);
			if (match.Success)
			{
				var hexChars = match.Captures[0].Value;
				return Convert.ToInt32(hexChars, 16);
			}

			return Convert.ToInt32(input);
		}
	}
}
