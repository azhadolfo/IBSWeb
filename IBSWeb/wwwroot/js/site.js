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

var table;

$(document).ready(function () {
    table = $('#dataTable').DataTable({
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

// start code for formatting of input type for tin number
document.addEventListener('DOMContentLoaded', () => {
    const inputFields = document.querySelectorAll('.formattedTinNumberInput');

    inputFields.forEach(inputField => {
        inputField.addEventListener('input', (e) => {
            let value = e.target.value.replace(/-/g, ''); // Remove existing dashes
            let formattedValue = '';

            // Add dashes after every 3 digits, keeping the last 5 digits without dashes
            for (let i = 0; i < value.length; i++) {
                if (i === 3 || i === 6 || i === 9) {
                    formattedValue += '-';
                }
                formattedValue += value[i];
            }

            // If there are more than 12 characters, don't add a dash after the 10th character (i.e., for the last 5 digits)
            if (formattedValue.length > 12) {
                formattedValue = formattedValue.substring(0, 12) + formattedValue.substring(12).replace(/-/g, '');
            }

            e.target.value = formattedValue;
        });

        inputField.addEventListener('keydown', (e) => {
            if (e.key === 'Backspace') {
                let value = e.target.value;
                // Remove the dash when backspace is pressed if it is at the end of a section of 3 digits
                if (value.endsWith('-')) {
                    e.target.value = value.slice(0, -1);
                }
            }
        });
    });
});
// end of code for formatting of input type for tin number
