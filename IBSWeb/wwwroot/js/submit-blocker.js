$(document).ready(function () {
    const spinnerWrapper = $('.loader-container');
    let isSubmitting = sessionStorage.getItem('isSubmitting') === 'true';

    // Move spinner to body if not already there
    if (!spinnerWrapper.parent().is('body')) {
        $('body').append(spinnerWrapper);
    }

    // If page was loaded during submission, show loader
    if (isSubmitting) {
        // Clear the flag immediately on the new page
        sessionStorage.removeItem('isSubmitting');
        spinnerWrapper.hide();
        $('body').removeClass('loading');
    } else {
        // Initially hide the spinner
        spinnerWrapper.hide();
    }

    $('form').on('submit', function (e) {
        // Get the submit button
        const submitButton = $(this).find('button[type="submit"], input[type="submit"]');

        // Check if form is already being submitted
        if (submitButton.prop('disabled')) {
            e.preventDefault();
            return false; // Prevent duplicate submission
        }

        $(this).validate();
        if (!$(this).valid()) {
            return false; // Stop execution if invalid
        }

        // Disable the submit button to prevent double clicks
        submitButton.prop('disabled', true);

        // Show the spinner wrapper when submitting
        spinnerWrapper.show();
        $('body').addClass('loading'); // Optional: prevent scrolling

        // Set flag for submission in progress
        sessionStorage.setItem('isSubmitting', 'true');

        // Let the form submit naturally - the spinner will show until the new page loads
        return true;
    });
});