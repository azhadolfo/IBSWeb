// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    $('.js-select2').select2({
        placeholder: "Select an option...",
        allowClear: true,
        width: 'resolve',
        theme: 'classic'
    });
});

// hack to fix jquery 3.6 focus security patch that bugs auto search in select-2
$(document).on('select2:open', () => {
    document.querySelector('.select2-search__field').focus();
});

$(document).ready(function () {
    $('#dataTable').DataTable();
});

function validateDate() {
    let dateFrom = document.getElementById("dateFrom").value;
    let dateTo = document.getElementById("dateTo").value;
    if (dateFrom > dateTo) {
        alert("Date From must be less than or equal to Date To");
        return false;
    }
    return true;
}