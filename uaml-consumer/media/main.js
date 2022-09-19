// This script will be run within the webview itself
// It cannot access the main VS Code APIs directly.


(function () {

        const vscode = acquireVsCodeApi();

        
        
        document.getElementsByTagName("svg")[0].setAttribute("id", "svgImage");
        const svgImage = /** @type {HTMLElement}} */ document.getElementById("svgImage");
        const svgContainer =/** @type {HTMLElement}} */ document.getElementById("svgContainer");
        
        const links =  /** @type {HTMLElement}} */ document.getElementsByTagName("a");
        
        
        var viewBox = {x:0, y:0, w:svgImage.clientWidth, h:svgImage.clientHeight};
        
        const svgSize = {w:svgImage.clientWidth,h:svgImage.clientHeight};
        console.log("set attr");
        svgImage.setAttribute('viewBox', `${viewBox.x} ${viewBox.y} ${viewBox.w} ${viewBox.h}`);
        var isPanning = false;
        var startPoint = {x:0,y:0};
        var endPoint = {x:0,y:0};;
        var scale = 1;
        
        window.addEventListener('message', event => {
                const message = event.data; // The JSON data our extension sent 
                switch (message.command) {
                        case 'viewBox':
                                if(message.viewBox !== undefined) {
                                        svgImage.setAttribute('viewBox', `${message.viewBox.x} ${message.viewBox.y} ${message.viewBox.w} ${message.viewBox.h}`);
                                        console.log(message.viewBox);
                                        viewBox = message.viewBox;

                                }
                        break;
                }
        });
        
        svgContainer.onwheel = function(e) {
                console.log("mouse wheel");
                e.preventDefault();
                var w = viewBox.w;
                var h = viewBox.h;
                var mx = e.offsetX;//mouse x  
                var my = e.offsetY;    
                var dw = w*Math.sign(-e.deltaY)*0.05;
                var dh = h*Math.sign(-e.deltaY)*0.05;
                var dx = dw*mx/svgSize.w;
                var dy = dh*my/svgSize.h;
                viewBox = {x:viewBox.x+dx,y:viewBox.y+dy,w:viewBox.w-dw,h:viewBox.h-dh};
                scale = svgSize.w/viewBox.w;
                zoomValue.innerText = `${Math.round(scale*100)/100}`;
                svgImage.setAttribute('viewBox', `${viewBox.x} ${viewBox.y} ${viewBox.w} ${viewBox.h}`);
                vscode.postMessage(
                        {
                                command: "view",
                                viewBox: viewBox
                        }
                );
        };


        svgContainer.onmousedown = function(e){
                console.log("mouse down");
                isPanning = true;
                startPoint = {x:e.x,y:e.y};  
                console.log("point: ", startPoint);
        };

        svgContainer.onmousemove = function(e){
                if (isPanning){
                        console.log("mouse move");
                        endPoint = {x:e.x,y:e.y};
                        var dx = (startPoint.x - endPoint.x)/scale;
                        var dy = (startPoint.y - endPoint.y)/scale;
                        var movedViewBox = {x:viewBox.x+dx,y:viewBox.y+dy,w:viewBox.w,h:viewBox.h};
                        svgImage.setAttribute('viewBox', `${movedViewBox.x} ${movedViewBox.y} ${movedViewBox.w} ${movedViewBox.h}`);
                        vscode.postMessage(
                                {
                                        command: "view",
                                        viewBox: movedViewBox
                                }
                        );
                }
        };

        svgContainer.onmouseup = function(e){
                if (isPanning){
                        console.log("mouse on up");
                        endPoint = {x:e.x,y:e.y};
                        var dx = (startPoint.x - endPoint.x)/scale;
                        var dy = (startPoint.y - endPoint.y)/scale;
                        console.log("before up: ", viewBox);
                        viewBox = {x:viewBox.x+dx,y:viewBox.y+dy,w:viewBox.w,h:viewBox.h};
                        console.log("up: ", viewBox);
                        svgImage.setAttribute('viewBox', `${viewBox.x} ${viewBox.y} ${viewBox.w} ${viewBox.h}`);
                        vscode.postMessage(
                        {
                                command: "view",
                                viewBox: viewBox
                        }
                        );
                        isPanning = false;
                }
        };
        
        svgContainer.onmouseleave = function(e){
                isPanning = false;
        };
        
        for(let l of links) {
                l.onclick = function(e) {
                        //e.defaultPrevented();
                        console.log("click");
                        let link = l.attributes.getNamedItem("href").textContent;
                        if(!link.startsWith("http") && !isPanning) {
                                 console.log("relative path");
                                 vscode.postMessage({
                                         command: 'relative',
                                         text: link
                                 });
                        }
                };
        }
        
}());