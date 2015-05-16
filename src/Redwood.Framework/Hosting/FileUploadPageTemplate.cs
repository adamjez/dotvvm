using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Redwood.Framework.Hosting
{
    public class FileUploadPageTemplate
    {
 
public string StartupScript { get; set; }

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
    <title></title>
	<meta charset=""utf-8"" />
</head>
<body>

<form method=""POST"" enctype=""multipart/form-data"" action=""/redwoodFileUpload"" id=""uploadForm"">
    <input type=""file"" name=""upload"" id=""upload"" multiple=""multiple"" onchange=""redwoodSubmit();""/>
</form>

<script type=""text/javascript"">
function redwoodSubmit() {
	var form = document.getElementById(""uploadForm"");

	if (window.FormData) {
		
		// send the form using AJAX
		var xhr = parent.window.redwood.getXHR();
		xhr.open(""POST"", form.action, true);
		xhr.setRequestHeader(""X-RedwoodAsyncUpload"", ""true"");
		xhr.upload.onprogress = function (e) {
			if (e.lengthComputable) {
				reportProgress(true, Math.round(e.loaded * 100 / e.total, 0), '');
			}
		};		
		xhr.onload = function(e) {
			if (xhr.status == 200) {
				reportProgress(false, 100, JSON.parse(xhr.responseText));
				form.reset();
			} else {
				reportProgress(false, 0, ""Upload failed."");
			}
		};
		xhr.send(new FormData(form));

	} else {

		// fallback for old browsers
		reportProgress(true, -1, '');	
		form.submit();

	}
}

function reportProgress(isBusy, percent, resultOrError) {
	parent.window.redwood.fileUpload.reportProgress(window.frameElement.dataset.targetControlId, isBusy, percent, resultOrError);
}

");
__sb.Append( StartupScript ?? "" );
__sb.Append(@"
</script>

</body>
</html>

");

            return __sb.ToString();
        }
    }
}
