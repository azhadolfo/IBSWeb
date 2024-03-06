// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    $('.js-select2').select2({
        width: 'resolve',
        theme: "classic"
    });
});

$(document).ready(function () {
    $('#dataTable').DataTable();
});