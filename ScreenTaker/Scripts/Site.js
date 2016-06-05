
function openNav() {
   
    $('#mySidenav').animate({
        width: "200px",
        opacity: "1"
    }, 50).promise().done(function () {
        $('#main').animate({
            marginRight: "200px"
        }, 200);
    });

    document.getElementById("openSideMenu").style.disabled = true;
    document.getElementById("openSideMenu").style.backgroundColor = "#AAAAAA";
    //$('#AddFolderButton').animate({
    //    marginRight: "-40px"
    //}, 500);
    //$('#UploadImageButton').animate({
    //    marginRight: "-40px"
    //}, 500);
    //$('#UploadImageSpan').animate({
    //    marginRight: "-50px"
    //}, 500);
}


function closeNav() {
    
    $('#mySidenav').animate({
        width: "0px",
        opacity: "0"
    }, 200).promise().done(function () {
        $('#main').animate({
            marginRight: "0px"
        }, 200);
    });

    document.getElementById("openSideMenu").style.disabled = false;
    document.getElementById("openSideMenu").style.backgroundColor = "white";
    //$('#AddFolderButton').animate({
    //    marginRight: "10px"
    //}, 500);
    //$('#UploadImageButton').animate({
    //    marginRight: "10px"
    //}, 500);
    //$('#UploadImageSpan').animate({
    //    marginRight: "-28px"
    //}, 500);
}

$('#toggle_event_editing button').click(function () {
    if ($(this).hasClass('locked_active') || $(this).hasClass('unlocked_inactive')) {
        /* code to do when unlocking */
        $('#switch_status').html('Switched on.');
    } else {
        /* code to do when locking */
        $('#switch_status').html('Switched off.');
    }

    /* reverse locking status */
    $('#toggle_event_editing button').eq(0).toggleClass('locked_inactive locked_active btn-default btn-info');
    $('#toggle_event_editing button').eq(1).toggleClass('unlocked_inactive unlocked_active btn-info btn-default');
});

function selectText(id) {
    document.getElementById(id).select();
}

function copyToClipboard(elem) {
    // create hidden text element, if it doesn't already exist
    var targetId = "_hiddenCopyText_";
    var isInput = elem.tagName === "INPUT" || elem.tagName === "TEXTAREA";
    var origSelectionStart, origSelectionEnd;
    if (isInput) {
        // can just use the original source element for the selection and copy
        target = elem;
        origSelectionStart = elem.selectionStart;
        origSelectionEnd = elem.selectionEnd;
    } else {
        // must use a temporary form element for the selection and copy
        target = document.getElementById(targetId);
        if (!target) {
            var target = document.createElement("textarea");
            target.style.position = "absolute";
            target.style.left = "-9999px";
            target.style.top = "0";
            target.id = targetId;
            document.body.appendChild(target);
        }
        target.textContent = elem.textContent;
    }
    // select the content
    var currentFocus = document.activeElement;
    target.focus();
    target.setSelectionRange(0, target.value.length);

    // copy the selection
    var succeed;
    try {
        succeed = document.execCommand("copy");
    } catch (e) {
        succeed = false;
    }
    // restore original focus
    if (currentFocus && typeof currentFocus.focus === "function") {
        currentFocus.focus();
    }

    if (isInput) {
        // restore prior selection
        elem.setSelectionRange(origSelectionStart, origSelectionEnd);
    } else {
        // clear temporary content
        target.textContent = "";
    }
    return succeed;
}