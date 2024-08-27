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

$(document).ready(function () {
    $('.js-multiple').select2({
        placeholder: "Select an option",
        allowClear: true,
        width: 'resolve'
    });
});

// hack to fix jquery 3.6 focus security patch that bugs auto search in select-2
$(document).on('select2:open', () => {
    document.querySelector('.select2-search__field').focus();
});

$(document).ready(function () {
    $('#dataTable').DataTable({
        stateSave: true,
        processing: true, // Enable the processing indicator
    });
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

function formatNumber(number) {
    return number.toLocaleString('en-US', { minimumFractionDigits: 4, maximumFractionDigits: 4 });
}

function parseNumber(formattedNum) {
    return parseFloat(formattedNum.replace(/,/g, '')) || 0;
}

function setTransactionDate() {
    // Get the current date in the format "YYYY-MM-DD" (required for the date input)
    var currentDate = new Date().toISOString().slice(0, 10);

    var transactionDateField = document.getElementById("TransactionDate");

    if (transactionDateField.value == '0001-01-01') {
        transactionDateField.value = currentDate;
    }
}

// Dynamic date to in books
document.addEventListener('DOMContentLoaded', function () {
    var dateFromInput = document.getElementById('DateFrom');
    var dateToInput = document.getElementById('DateTo');

    // Add an event listener to DateFrom input
    dateFromInput?.addEventListener('change', function () {
        // Set DateTo input value to DateFrom input value
        dateToInput.value = dateFromInput.value;
    });
});