// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function submitAnswer(questionNum) {
    let answer = ''
    if ($('#answer')) {
        answer = $('#answer').val();
        answer = answer.replace(/[^a-z0-9]/gi, '');
    }
    $.ajax({
        type: "POST",
        url: '/Home/ValidateAnswer',
        data: JSON.stringify({ qno: questionNum, answer: answer }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: successFunc,
    });

    function successFunc(data, status) {
        if (!data) {
            alert('Wrong Answer. Try Again !')
        }
        else {
            window.location = '/'
        }
    }
};

$(function () {
    $("#qnaForm").submit(function () { return false; });
});

$('#answer').keypress(function (e) {
    if (e.which == 13) {
        $('#submit').click();
    }
});


