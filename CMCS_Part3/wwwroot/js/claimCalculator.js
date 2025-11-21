function calculateTotal() {
    const hours = parseFloat(document.getElementById('HoursWorked').value) || 0;
    const rate = parseFloat(document.getElementById('HourlyRate').value) || 0;
    const total = hours * rate;

    document.getElementById('totalAmount').textContent = 'R ' + total.toFixed(2);

    // Validation checks
    validateHours(hours);
    validateRate(rate);
}

function validateHours(hours) {
    const errorElement = document.getElementById('hoursError');
    if (hours < 1 || hours > 176) {
        errorElement.textContent = 'Hours must be between 1 and 176';
        return false;
    }
    errorElement.textContent = '';
    return true;
}

function validateRate(rate) {
    const errorElement = document.getElementById('rateError');
    if (rate < 100 || rate > 1000) {
        errorElement.textContent = 'Hourly rate must be between R100 and R1000';
        return false;
    }
    errorElement.textContent = '';
    return true;
}

// Real-time calculation
document.addEventListener('DOMContentLoaded', function () {
    const hoursInput = document.getElementById('HoursWorked');
    const rateInput = document.getElementById('HourlyRate');

    if (hoursInput && rateInput) {
        hoursInput.addEventListener('input', calculateTotal);
        rateInput.addEventListener('input', calculateTotal);

        // Initial calculation
        calculateTotal();
    }
});