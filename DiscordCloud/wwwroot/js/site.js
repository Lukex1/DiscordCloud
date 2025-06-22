window.downloadFile = function (fileData, fileName) {
    const blob = new Blob([fileData], { type: 'application/octet-stream' });
    const link = document.createElement("a");
    link.href = URL.createObjectURL(blob);
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

window.confirmDelete = (message) => {
    return confirm(message);
};