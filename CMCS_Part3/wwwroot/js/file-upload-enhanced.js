// Enhanced file upload functionality with drag & drop
document.addEventListener('DOMContentLoaded', function () {
    const fileInput = document.getElementById('fileInput');
    const filePreview = document.getElementById('filePreview');
    const dropZone = document.getElementById('dropZone');
    const uploadProgress = document.getElementById('uploadProgress');
    const supportedFileTypes = ['.pdf', '.docx', '.xlsx'];
    const maxFileSize = 5 * 1024 * 1024; // 5MB

    // Initialize file upload functionality
    function initializeFileUpload() {
        if (fileInput && dropZone) {
            // Click on drop zone to trigger file input
            dropZone.addEventListener('click', function () {
                fileInput.click();
            });

            // File input change event
            fileInput.addEventListener('change', handleFileSelection);

            // Drag and drop events
            ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
                dropZone.addEventListener(eventName, preventDefaults, false);
            });

            ['dragenter', 'dragover'].forEach(eventName => {
                dropZone.addEventListener(eventName, highlight, false);
            });

            ['dragleave', 'drop'].forEach(eventName => {
                dropZone.addEventListener(eventName, unhighlight, false);
            });

            dropZone.addEventListener('drop', handleDrop, false);
        }
    }

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    function highlight() {
        dropZone.classList.add('bg-light', 'border-primary');
    }

    function unhighlight() {
        dropZone.classList.remove('bg-light', 'border-primary');
    }

    function handleDrop(e) {
        const dt = e.dataTransfer;
        const files = dt.files;
        handleFiles(files);
    }

    function handleFileSelection(e) {
        const files = e.target.files;
        handleFiles(files);
    }

    function handleFiles(files) {
        if (!files || files.length === 0) return;

        // Clear previous previews
        if (filePreview) {
            filePreview.innerHTML = '';
        }

        // Process each file
        for (let i = 0; i < files.length; i++) {
            const file = files[i];
            if (validateFile(file)) {
                createFilePreview(file);
            }
        }

        // Update file input to include all valid files
        updateFileInput();
    }

    function validateFile(file) {
        // Check file type
        const fileExtension = '.' + file.name.split('.').pop().toLowerCase();
        if (!supportedFileTypes.includes(fileExtension)) {
            showError(`File type not supported: ${file.name}. Supported types: ${supportedFileTypes.join(', ')}`);
            return false;
        }

        // Check file size
        if (file.size > maxFileSize) {
            showError(`File too large: ${file.name}. Maximum size: ${formatFileSize(maxFileSize)}`);
            return false;
        }

        return true;
    }

    function createFilePreview(file) {
        if (!filePreview) return;

        // Generate unique ID
        const fileId = 'file-' + Date.now() + '-' + Math.random().toString(36).substring(2, 11);
        const fileSize = formatFileSize(file.size);
        const fileType = file.name.split('.').pop().toUpperCase();

        const previewElement = document.createElement('div');
        previewElement.className = 'file-preview alert alert-info d-flex justify-content-between align-items-center';
        previewElement.id = fileId;
        previewElement.innerHTML = `
            <div>
                <i class="fas fa-file me-2"></i>
                <strong>${file.name}</strong> 
                <small class="text-muted">(${fileSize}) - ${fileType}</small>
            </div>
            <button type="button" class="btn btn-sm btn-outline-danger" onclick="removeFilePreview('${fileId}')">
                <i class="fas fa-times"></i>
            </button>
        `;

        filePreview.appendChild(previewElement);
    }

    function removeFilePreview(fileId) {
        const element = document.getElementById(fileId);
        if (element) {
            element.remove();
        }
        updateFileInput();
    }

    function updateFileInput() {
        if (!fileInput) return;

        // Create a new DataTransfer object
        const dt = new DataTransfer();

        // Get all current file previews
        const filePreviews = document.querySelectorAll('.file-preview');

        console.log('Files ready for upload:', filePreviews.length);
    }

    function showError(message) {
        if (!filePreview) return;

        const errorElement = document.createElement('div');
        errorElement.className = 'alert alert-danger alert-dismissible fade show';
        errorElement.innerHTML = `
            <i class="fas fa-exclamation-triangle"></i> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        filePreview.appendChild(errorElement);
    }

    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    // Make removeFilePreview function globally available
    window.removeFilePreview = removeFilePreview;

    // Initialize when page loads
    initializeFileUpload();
});

// Progress bar animation for upload simulation
function simulateUploadProgress(progressBar, duration = 2000) {
    let progress = 0;
    const interval = 50;
    const increment = (interval / duration) * 100;

    const timer = setInterval(() => {
        progress += increment;
        if (progress >= 100) {
            progress = 100;
            clearInterval(timer);
        }
        progressBar.style.width = progress + '%';
        progressBar.setAttribute('aria-valuenow', progress);
    }, interval);
}