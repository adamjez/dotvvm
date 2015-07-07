using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace DotVVM.Framework.Hosting
{
    public class ErrorPageTemplate
    {

	public int ErrorCode { get; set; }

	public string ErrorDescription { get; set; }

	public string FileName { get; set; }

	public Type ClassName { get; set; }

	public int LineNumber { get; set; }

	public int PositionOnLine { get; set; }

	public string Url { get; set; }

	public string Verb { get; set; }

	public string IpAddress { get; set; }

	public string CurrentUserName { get; set; }

	public Exception Exception { get; set; }


	private void WriteException(Exception ex) 
	{
		using (var sr = new StringReader(ex.ToString()))
		{
			string line;
			while ((line = sr.ReadLine()) != null)
			{
				this.Write(WebUtility.HtmlEncode(line));
				this.Write("<br />");
			}
		}
	}


	/// <summary>
	/// Gets the source lines near the error and highlights the error.
	/// </summary>
    private IList<SourceLine> GetSourceLines()
	{
	    if (string.IsNullOrEmpty(FileName))
	    {
	        return null;
	    }

        try
        {
            var lines = new List<SourceLine>();
			using (var fs = new FileStream(FileName, FileMode.Open, FileAccess.Read))
			{
				using (var sr = new StreamReader(fs, true))
				{
					string line;
					int lineNumber = 0;
					while (!sr.EndOfStream && lineNumber < LineNumber + 2)
					{
						line = sr.ReadLine();
						lineNumber++;

						if (lineNumber >= LineNumber - 2)
						{
							// write the line to the output
							lines.Add(new SourceLine() { LineNumber = lineNumber, Text = line });

							// mark the position on the problem line
							if (lineNumber == LineNumber)
							{
								lines.Add(new SourceLine() { Text = new string('-', PositionOnLine) + '^' });
							}
						}
					}
				}
			}
            return lines;
        }
        catch
        {
            return null;
        } 
    }


    class SourceLine
    {
        public int? LineNumber { get; set; }

        public string Text { get; set; }
	}

        private System.Text.StringBuilder __sb;

        private void Write(string text) {
            __sb.Append(text);
        }

        private void WriteLine(string text) {
            __sb.AppendLine(text);
        }

        public string TransformText()
        {
            __sb = new System.Text.StringBuilder();
__sb.Append(@"

<!DOCTYPE html>
<html>
	<head>
		<title>Server Error in Application</title>
		<style type=""text/css"">
body { font-family: Arial,Tahoma,sans-serif; font-size: 11pt; }
h1 { color: #e00000; font-weight: normal; font-size: 24pt; }
h2 { font-style: normal; font-size: 16pt; font-weight: bold; margin-bottom: 35px; }
h3 { color: #e00000; font-weight: normal; font-size: 14pt; }
pre { background-color: #ffffc0; padding: 20px; font-size: 12pt; }
span.current-line { color: red; }
		</style>
	</head>
	<body>
		<h1>Server Error in Application</h1>
		<h2>HTTP ");
__sb.Append( ErrorCode );
__sb.Append(@" ");
__sb.Append( WebUtility.HtmlEncode(ErrorDescription) );
__sb.Append(@"</h2>
		<p><strong>");
__sb.Append( Exception.GetType().FullName );
__sb.Append(@": ");
__sb.Append( WebUtility.HtmlEncode(Exception.Message) );
__sb.Append(@"</strong></p>

");
 if (Url != null) { __sb.Append(@"
		<p>Request URL: <strong>");
__sb.Append( Verb );
__sb.Append(@" ");
__sb.Append( WebUtility.HtmlEncode(Url) );
__sb.Append(@"</strong></p>
");
 } __sb.Append(@"

");
 if (ClassName != null) { __sb.Append(@"
		<p>Source Class: <strong>");
__sb.Append( WebUtility.HtmlEncode(ClassName.AssemblyQualifiedName) );
__sb.Append(@"</strong></p>
");
 } __sb.Append(@"

");
 if (!string.IsNullOrEmpty(FileName)) { __sb.Append(@"
		<p>Source File: <strong>");
__sb.Append( WebUtility.HtmlEncode(FileName) );
__sb.Append(@"</strong></p>
");
 } __sb.Append(@"

");
 if (LineNumber > 0) { __sb.Append(@"
		<p>Line: <strong>");
__sb.Append( LineNumber );
__sb.Append(@"</strong></p>
");
 } __sb.Append(@"

");
 if (!string.IsNullOrEmpty(CurrentUserName)) { __sb.Append(@"
		<p>Current User: <strong>");
__sb.Append( WebUtility.HtmlEncode(CurrentUserName) );
__sb.Append(@"</strong></p>
");
 } __sb.Append(@"

");
 if (!string.IsNullOrEmpty(IpAddress)) { __sb.Append(@"
		<p>Client IP: <strong>");
__sb.Append( WebUtility.HtmlEncode(IpAddress) );
__sb.Append(@"</strong></p>
");
 } __sb.Append(@"

");
 
	var sourceLines = GetSourceLines();
    if (sourceLines != null)
    {
__sb.Append(@"
		<p>&nbsp;</p>
		<h3>Source Location</h3>
");

        this.Write("<pre>");
        foreach (var line in sourceLines)
        {
            if (line.LineNumber == LineNumber)
            {
                this.Write("<span class='current-line'>");
            }

            if (line.LineNumber == null)
            {
                this.Write(new string(' ', 15));
            }
            else
            {
                this.Write(("Line " + line.LineNumber + ": ").PadLeft(15));
            }
            this.Write(WebUtility.HtmlEncode(line.Text));

			if (line.LineNumber == LineNumber)
            {
                this.Write("</span>");
            }
            this.Write("<br />");
        }
		this.Write("</pre>");
    }
__sb.Append(@"
		
		<p>&nbsp;</p>
		<h3>Stack Trace</h3>
");

    this.Write("<pre>");
    WriteException(Exception);

	if (Exception is ReflectionTypeLoadException) {
		var loaderExceptions = ((ReflectionTypeLoadException)Exception).LoaderExceptions;
		foreach (var ex in loaderExceptions) {
			this.Write("<br />");
			WriteException(ex);
		}
	}

	this.Write("</pre>");
__sb.Append(@"</pre>
		<p>&nbsp;</p>

	</body>
</html>




");

            return __sb.ToString();
        }
    }
}
