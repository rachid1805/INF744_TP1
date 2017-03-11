using System;
using System.IO;
using System.Xml;

namespace DataLinkApplication
{
  public sealed class XmlDataLinkConfigFileReader
  {
    #region Constants

    private const string DocumentTag = "Document";
    private const string DocumentTitleAttribName = "Title";
    private const string DocumentVersionAttribName = "Version";
    private const string DocumentTitleValue = "Fichier de commandes pour l'application";
    private const string DocumentVersionValue = "1.0";
    private const string DataLinkCommandsTag = "DataLinkCommands";
    private const string InFileTag = "InFile";
    private const string OutFileTag = "OutFile";
    private const string WindowSizeTag = "WindowSize";
    private const string RejectTypeTag = "RejectType";
    private const string TimeoutTag = "Timeout";
    private const string AckTimeoutTag = "AckTimeout";
    private const string LatencyTag = "Latency";
    private const string ProbabilityErrorTag = "ProbabilityError";
    private const string NumberOfBitErrorsTag = "NumberOfBitErrors";

    #endregion

    #region Constructor

    public XmlDataLinkConfigFileReader(string  xmlFileNameAndPath)
    {
      ReadXmlFile(xmlFileNameAndPath);
    }

    #endregion

    #region Private Functions

    private void ReadXmlFile(string xmlFilepath)
    {
      if (!File.Exists(xmlFilepath))
      {
        throw new FileNotFoundException(string.Format("Could not find file: {0}", xmlFilepath));
      }

      // First, try to create an XmlReader for the specified file path.
      using (var xmlReader = XmlReader.Create(xmlFilepath))
      {
        var documentTagFound = false;
        var dataLinkCommandsTagFound = false;
        var docTitle = string.Empty;
        var docVersion = string.Empty;
        var windowSizeStr = string.Empty;
        var rejectTypeStr = string.Empty;
        var timeoutStr = string.Empty;
        var ackTimeoutStr = string.Empty;
        var latencyStr = string.Empty;
        var probabilityErrorStr = string.Empty;
        var numberOfBitErrorsStr = string.Empty;

        // Parse the file and collect required attributes and values. 
        while (xmlReader.Read())
        {
          if (xmlReader.IsStartElement())
          {
            switch (xmlReader.Name)
            {
              case DocumentTag:
                documentTagFound = true;
                // if an attribute is not found, the value will be Null
                docTitle = xmlReader[DocumentTitleAttribName];
                docVersion = xmlReader[DocumentVersionAttribName];
                break;
              case DataLinkCommandsTag:
                dataLinkCommandsTagFound = true;
                break;
              case InFileTag:
                InFile = ReadValue(xmlReader);
                break;
              case OutFileTag:
                OutFile = ReadValue(xmlReader);
                break;
              case WindowSizeTag:
                windowSizeStr = ReadValue(xmlReader);
                break;
              case RejectTypeTag:
                rejectTypeStr = ReadValue(xmlReader);
                break;
              case TimeoutTag:
                timeoutStr = ReadValue(xmlReader);
                break;
              case AckTimeoutTag:
                ackTimeoutStr = ReadValue(xmlReader);
                break;
              case LatencyTag:
                latencyStr = ReadValue(xmlReader);
                break;
              case ProbabilityErrorTag:
                probabilityErrorStr = ReadValue(xmlReader);
                break;
              case NumberOfBitErrorsTag:
                numberOfBitErrorsStr = ReadValue(xmlReader);
                break;
            }
          }
        }

        // Validate the information retrieved and throw if missing or invalid.
        if (!documentTagFound)
        {
          throw new ApplicationException("Unrecognized XML file format provided: No Document Tag Found!");
        }

        if (string.IsNullOrEmpty(docTitle))
        {
          throw new ApplicationException("Unrecognized XML file format provided: Document Tag missing Title attribute!");
        }

        if (string.IsNullOrEmpty(docVersion))
        {
          throw new ApplicationException("Unrecognized XML file format provided: Document Tag missing Version attribute!");
        }

        if (!docTitle.Equals(DocumentTitleValue))
        {
          throw new ApplicationException("Unrecognized XML file format provided: Invalid Document Title!");
        }

        if (!docVersion.Equals(DocumentVersionValue))
        {
          throw new ApplicationException("Unrecognized XML file format provided: Invalid Document Version!");
        }

        if (!dataLinkCommandsTagFound)
        {
          throw new ApplicationException("Unrecognized XML file format provided: DataLinkCommands Tag Not Found!");
        }

        if (string.IsNullOrEmpty(windowSizeStr))
        {
          throw new ApplicationException("Unrecognized XML file format provided: WindowSize Tag Not Found!");
        }
        WindowSize = Byte.Parse(windowSizeStr);

        if (string.IsNullOrEmpty(rejectTypeStr))
        {
          throw new ApplicationException("Unrecognized XML file format provided: RejectType Tag Not Found!");
        }
        RejectType = rejectTypeStr.Equals("Global") ? RejectType.Global : RejectType.Selective;

        if (string.IsNullOrEmpty(timeoutStr))
        {
          throw new ApplicationException("Unrecognized XML file format provided: Timeout Tag Not Found!");
        }
        Timeout = Int32.Parse(timeoutStr);

        if (string.IsNullOrEmpty(ackTimeoutStr))
        {
          throw new ApplicationException("Unrecognized XML file format provided: AckTimeout Tag Not Found!");
        }
        AckTimeout = Int32.Parse(ackTimeoutStr);

        if (string.IsNullOrEmpty(latencyStr))
        {
          throw new ApplicationException("Unrecognized XML file format provided: Latency Tag Not Found!");
        }
        Latency = Int32.Parse(latencyStr);

        if (string.IsNullOrEmpty(probabilityErrorStr))
        {
          throw new ApplicationException("Unrecognized XML file format provided: ProbabilityError Tag Not Found!");
        }
        ProbabilityError = Byte.Parse(probabilityErrorStr);

        if (string.IsNullOrEmpty(numberOfBitErrorsStr))
        {
          throw new ApplicationException("Unrecognized XML file format provided: ErrorBitNumber Tag Not Found!");
        }
        NumberOfBitErrors = Byte.Parse(numberOfBitErrorsStr);
      }
    }

    private static string ReadValue(XmlReader xmlReader)
    {
      if (!xmlReader.IsEmptyElement)
      {
        xmlReader.Read();
        if (xmlReader.NodeType == XmlNodeType.Text)
        {
          return xmlReader.Value;
        }
      }
      return null;
    }

    #endregion

    #region Properties

    public string InFile { get; private set; }
    public string OutFile { get; private set; }
    public byte WindowSize { get; private set; }
    public RejectType RejectType { get; private set; }
    public int Timeout { get; private set; }
    public int AckTimeout { get; private set; }
    public int Latency { get; private set; }
    public byte ProbabilityError { get; private set; }
    public byte NumberOfBitErrors { get; private set; }

    #endregion
  }
}
