
function openNav() {
   
    $('#mySidenav').animate({
        width: "260px",
        opacity: "1"
    }, 50).promise().done(function () {
        $('#main').animate({
            marginRight: "260px"
        }, 200);
    });

    document.getElementById("openSideMenu").style.visibility = "collapse";
    $('#AddFolderButton').animate({
        marginRight: "-40px"
    }, 500);
    $('#UploadImageButton').animate({
        marginRight: "-40px"
    }, 500);
    $('#UploadImageSpan').animate({
        marginRight: "-50px"
    }, 500);
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

    document.getElementById("openSideMenu").style.visibility = "visible";
    $('#AddFolderButton').animate({
        marginRight: "10px"
    }, 500);
    $('#UploadImageButton').animate({
        marginRight: "10px"
    }, 500);
    $('#UploadImageSpan').animate({
        marginRight: "-28px"
    }, 500);
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

function selectText() {
    document.getElementById('sharedLink').select();
}

function openImage(path) {
    location.href = "/Home/SingleImage?image=" + path;
    return false;
}

function selectImage(id, image_id, count, sharedCode, original, sharedLink, compressed, name) {
    var classes = "col-md-2 text-center task active images";
    document.getElementById("original").value = original;
    document.getElementById("sharedLink").value = sharedLink;
    document.getElementById("compressedLink").src = compressed;
    document.getElementById("compressedName").innerText = name;

    document.getElementById("sharedLinkhidden").value = sharedCode;
    document.getElementById("Idhidden").value = id;
    for (var i = 0; i < count; i++) {
        var ID = "ID" + i;
        document.getElementById(ID).className = classes;
    }
    document.getElementById(image_id).className += " active-image";
    return false;
}

function openFolder(i) {
    var id = document.getElementById("Idhidden").value;
    if (id != "") i = id;
    location.href = "/Home/Images?id=" + i;
    return false;
}

function selectFolder(id, folder_id, count, name, sharedLinkValue, sharedCode, image) {
    document.getElementById("sharedLink").value = sharedLinkValue;

    document.getElementById("sharedLinkhidden").value = sharedCode;
    document.getElementById("Idhidden").value = id;

    document.getElementById("folderImage").src = image;
    document.getElementById("folderTitle").innerText = name;

    var classes = "col-md-2 text-center task active images folder";
    for (var i = 0; i < count; i++) {
        var ID = "FID" + i;
        document.getElementById(ID).className = classes;
    }
    document.getElementById(folder_id).className += " active-image";

    return false;
}