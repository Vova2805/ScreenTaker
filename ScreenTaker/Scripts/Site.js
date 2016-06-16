    
function openNav() {
    $('#openSideMenu').animate({
        opacity: "0"
    }, 10);
    $('#AddFolderButton').animate({
        opacity: "0"
    }, 10);
    $('#mySidenav').animate({
        width: "200px",
        opacity: "1"
    }, 50).promise().done();
    document.getElementById("openSideMenu").disabled = true;
    $('#openSideMenu').removeClass("open-button-closed");
    $('#openSideMenu').addClass("open-button-open");
    $('#parent-container').removeClass("main-area-closed");
    $('#parent-container').addClass("main-area-open");
    $('#parent-container-img').removeClass("main-area-closed");
    $('#parent-container-img').addClass("main-area-open");
    $('#SingleMainArea').removeClass("main-area-closed");
    $('#SingleMainArea').addClass("main-area-open");
    $('#navigation').removeClass("navigation-closed");
    $('#navigation').addClass("navigation-open");
    $('#singleImageNavigation').removeClass("navigation-closed");
    $('#singleImageNavigation').addClass("navigation-open");
    $('#openSideMenu').animate({
        opacity: "1"
    }, 30);
    $('#AddFolderButton').animate({
        opacity: "1"
    }, 30);
}


function closeNav() {
    $('#openSideMenu').animate({
        opacity: "0"
    }, 10);
    $('#AddFolderButton').animate({
        opacity: "0"
    }, 10);
    $('#mySidenav').animate({
        width: "0px",
        opacity: "0"
    }, 200).promise().done();
    document.getElementById("openSideMenu").disabled = false;
    $('#openSideMenu').addClass("open-button-closed");
    $('#openSideMenu').removeClass("open-button-open");
    $('#parent-container').addClass("main-area-closed");
    $('#parent-container').removeClass("main-area-open");
    $('#parent-container-img').addClass("main-area-closed");
    $('#parent-container-img').removeClass("main-area-open");
    $('#SingleMainArea').addClass("main-area-closed");
    $('#SingleMainArea').removeClass("main-area-open");
    $('#navigation').addClass("navigation-closed");
    $('#navigation').removeClass("navigation-open");
    $('#singleImageNavigation').addClass("navigation-closed");
    $('#singleImageNavigation').removeClass("navigation-open");
    $('#openSideMenu').animate({
        opacity: "1"
    }, 30);
    $('#AddFolderButton').animate({
        opacity: "1"
    }, 30);
}

function selectText() {
    document.getElementById('sharedLink').select();
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
    //$('#SuccessCopy').modal('show');
    return succeed;
}
function emailValidation(email) {
    var pattern = /^[-a-z0-9~!$%^&*_=+}{\'?]+(\.[-a-z0-9~!$%^&*_=+}{\'?]+)*@([a-z0-9_][-a-z0-9_]*(\.[-a-z0-9_]+)*\.(aero|arpa|biz|com|coop|edu|gov|info|int|mil|museum|name|net|org|pro|travel|mobi|[a-z][a-z])|([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}))(:[0-9]{1,5})?$/;
    if (email == ''|| !pattern.test(email))
    {
    return false;
    }
    return true;
}

function passwordValidation(pass) {
    var pattern = /(?=.*[a-zA-Z0-9]).{6,}/;
    if (pass == '' || !pattern.test(pass)) {
        return false;
    }
    return true;
}

function ConfirmPasswordValidation(confirmed, pass) {
    if (confirmed == pass) return true;
    return false;
}
function setClasses(id, respond) {
    if (respond == false) {
        $(id).removeClass('field-success');
        $(id).addClass('field-error');
    }
    else {
        $(id).removeClass('field-error');
        $(id).addClass('field-success');
    }
}
function localizationOver() {
    $('#dropdown-menu').slideToggle();
}
function localizationLeave() {
    $('#dropdown-menu').slideUp();
}