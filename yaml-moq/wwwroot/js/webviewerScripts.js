window.webviewerFunctions = {
    initWebViewer: function (data) {
        const viewerElement = document.getElementById('viewer');

        WebViewer({
            path: 'lib',
        }, viewerElement).then(instance => {

            const arr = new Uint8Array(data);
            const blob = new Blob([arr], { type: 'application/pdf' });
            instance.UI.loadDocument(blob, { filename: 'myfile.pdf' });

            const { documentViewer } = instance.Core;
            documentViewer.addEventListener('documentLoaded', () => {
                // perform document operations
            });
        });
    }
};