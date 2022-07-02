/*Copyright © 2022 SLT World.All rights reserved.
Use of this source code is governed by a BSD - style license that can be found in the LICENSE file.*/

var htmlbox = document.querySelector(".HTMLTextArea");
var cssbox = document.querySelector(".CSSTextArea");
var jsbox = document.querySelector(".JSTextArea");
function CompileCodeEditor() {
    var html = htmlbox.value;
    /*var css = "";
    if(cssbox.value == "")
      css = "<style>" + "body { font-family: \"Lucida Grande\",Verdana; }" + "</style>";*/
    var css = "<style>" + cssbox.value + "</style>";
    var js = "<script>" + jsbox.value + "</script>";
    var frame = document.querySelector(".CodeDisplayFrame").contentWindow.document;
    frame.open();
    frame.write(html + css + js);
    frame.close();
}

function Download() {
    var html = htmlbox.value;
    var css = "<style>" + cssbox.value + "</style>";
    var js = "<script>" + jsbox.value + "</script>";
    download("htmleditorscript.html", html + css + js);
}

function download(filename, text) {
    var element = document.createElement('a');
    element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(text));
    element.setAttribute('download', filename);

    element.style.display = 'none';
    document.body.appendChild(element);

    element.click();

    document.body.removeChild(element);
}