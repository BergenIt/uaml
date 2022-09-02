using Microsoft.JSInterop;

namespace YamlMockup.Core;

public static class FileUtil
{
    public static ValueTask<object> SaveAs(this IJSRuntime js, string filename, byte[] data)
       => js.InvokeAsync<object>(
           "saveAsFile",
           filename,
           Convert.ToBase64String(data));

    public static ValueTask<object> ViewPdf(this IJSRuntime js, byte[] data)
    {
        return js.InvokeAsync<object>(
               "webviewerFunctions.initWebViewer",
               data
        );
    }
}