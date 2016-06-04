
function openNav() {
   
    $('#mySidenav').animate({
        width: "200px",
        opacity: "1"
    }, 50).promise().done(function () {
        $('#main').animate({
            marginRight: "200px"
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
