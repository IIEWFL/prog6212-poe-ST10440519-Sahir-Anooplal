// Real-time automation for claim submission form
document.addEventListener('DOMContentLoaded', function () {
    const hoursInput = document.getElementById('HoursWorked');
    const rateInput = document.getElementById('HourlyRate');
    const totalDisplay = document.getElementById('totalAmountDisplay');
    const calculationCard = document.getElementById('calculationCard');
    const monthSelect = document.getElementById('ClaimMonth');
    const submitButton = document.querySelector('button[type="submit"]');

    // Initialize calculation display
    if (calculationCard) {
        calculationCard.style.display = 'none';
    }

    // Function to calculate and display total
    function calculateTotal() {
        const hours = parseFloat(hoursInput?.value) || 0;
        const rate = parseFloat(rateInput?.value) || 0;
        const total = hours * rate;

        if (totalDisplay) {
            totalDisplay.textContent = formatCurrency(total);
        }

        // Show/hide calculation card based on input
        if (calculationCard) {
            calculationCard.style.display = (hours > 0 && rate > 0) ? 'block' : 'none';
        }

        // Add validation styling
        validateInputs(hours, rate);

        // Update calculation details
        updateCalculationDetails(hours, rate);
    }

    // Format currency as South African Rand
    function formatCurrency(amount) {
        return 'R ' + amount.toLocaleString('en-ZA', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    // Update calculation details
    function updateCalculationDetails(hours, rate) {
        const details = document.getElementById('calculationDetails');
        if (details && hours > 0 && rate > 0) {
            details.textContent = `${hours} hours × R${rate.toFixed(2)}/hour = R${(hours * rate).toFixed(2)}`;
        } else if (details) {
            details.textContent = 'Enter hours and rate to see calculation';
        }
    }

    // Real-time input validation
    function validateInputs(hours, rate) {
        const hoursError = document.getElementById('hoursError');
        const rateError = document.getElementById('rateError');

        // Hours validation
        if (hoursInput) {
            if (hours > 200) {
                hoursInput.classList.add('is-invalid');
                hoursInput.classList.remove('is-valid');
                if (hoursError) hoursError.textContent = 'Maximum 200 hours per month';
            } else if (hours < 1) {
                hoursInput.classList.add('is-invalid');
                hoursInput.classList.remove('is-valid');
                if (hoursError) hoursError.textContent = 'Minimum 1 hour required';
            } else if (hours > 0) {
                hoursInput.classList.remove('is-invalid');
                hoursInput.classList.add('is-valid');
                if (hoursError) hoursError.textContent = '';
            }
        }

        // Rate validation
        if (rateInput) {
            if (rate > 1000) {
                rateInput.classList.add('is-invalid');
                rateInput.classList.remove('is-valid');
                if (rateError) rateError.textContent = 'Maximum rate is R1000 per hour';
            } else if (rate < 100) {
                rateInput.classList.add('is-invalid');
                rateInput.classList.remove('is-valid');
                if (rateError) rateError.textContent = 'Minimum rate is R100 per hour';
            } else if (rate > 0) {
                rateInput.classList.remove('is-invalid');
                rateInput.classList.add('is-valid');
                if (rateError) rateError.textContent = '';
            }
        }

        // Enable/disable submit button based on validation
        if (submitButton) {
            const isValid = hours >= 1 && hours <= 200 && rate >= 100 && rate <= 1000;
            submitButton.disabled = !isValid;
        }
    }

    // Event listeners for real-time calculation
    if (hoursInput && rateInput) {
        hoursInput.addEventListener('input', calculateTotal);
        rateInput.addEventListener('input', calculateTotal);

        // Initial calculation
        calculateTotal();
    }

    // Month selection formatting
    if (monthSelect) {
        monthSelect.addEventListener('change', function () {
            const selectedDate = new Date(this.value + '-01');
            const monthName = selectedDate.toLocaleString('en-ZA', {
                month: 'long',
                year: 'numeric'
            });

            const monthDisplay = document.getElementById('selectedMonthDisplay');
            if (monthDisplay) {
                monthDisplay.textContent = monthName;
            }
        });

        // Trigger change event to set initial month display
        monthSelect.dispatchEvent(new Event('change'));
    }

    // Auto-format currency input
    if (rateInput) {
        rateInput.addEventListener('blur', function () {
            const value = parseFloat(this.value);
            if (!isNaN(value)) {
                this.value = value.toFixed(2);
            }
        });
    }

    // File upload preview and validation
    function setupFileUpload() {
        const fileInput = document.querySelector('input[type="file"]');
        const filePreview = document.getElementById('filePreview');

        if (fileInput && filePreview) {
            fileInput.addEventListener('change', function (e) {
                const file = e.target.files[0];
                if (file) {
                    const fileSize = formatFileSize(file.size);
                    const fileType = file.name.split('.').pop().toUpperCase();

                    filePreview.innerHTML = `
                        <div class="alert alert-info">
                            <i class="fas fa-file"></i>
                            <strong>${file.name}</strong> (${fileSize}) - ${fileType}
                            <br><small class="text-muted">Ready for upload</small>
                        </div>
                    `;

                    // Validate file size
                    if (file.size > 5 * 1024 * 1024) {
                        filePreview.innerHTML = `
                            <div class="alert alert-danger">
                                <i class="fas fa-exclamation-triangle"></i>
                                File too large! Maximum size is 5MB.
                            </div>
                        `;
                        fileInput.value = '';
                    }
                } else {
                    filePreview.innerHTML = '';
                }
            });
        }
    }

    // Format file size for display
    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    // Initialize file upload
    setupFileUpload();

    // Form submission handling
    const form = document.querySelector('form');
    if (form) {
        form.addEventListener('submit', function (e) {
            const hours = parseFloat(hoursInput?.value) || 0;
            const rate = parseFloat(rateInput?.value) || 0;

            if (hours < 1 || hours > 200) {
                e.preventDefault();
                alert('Please enter valid hours (1-200) before submitting.');
                hoursInput?.focus();
                return;
            }

            if (rate < 100 || rate > 1000) {
                e.preventDefault();
                alert('Please enter a valid hourly rate (R100-R1000) before submitting.');
                rateInput?.focus();
                return;
            }

            // Show loading state
            if (submitButton) {
                submitButton.disabled = true;
                submitButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Submitting...';
            }
        });
    }
});