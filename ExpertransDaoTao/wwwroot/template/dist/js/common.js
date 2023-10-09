function sdtInit(elementId, pageLength) {
    var length = 10;
    if (pageLength) {
        length = pageLength;
    }
    $(elementId).DataTable({
        responsive: true,
        language: {
            emptyTable: "Không có bản ghi nào để hiển thị",
            info: "Hiển thị _START_ đến _END_ trong _TOTAL_ bản ghi",
            lengthMenu: "Hiển thị _MENU_ bản ghi",
            search: "Tìm kiếm:",
            paginate: {
                "first": "First",
                "last": "Last",
                "next": ">>",
                "previous": "<<"
            }
        },
        pageLength: length
    });
}

function getStudentById(id, callback) {
    $.ajax({
        type: "get",
        url: "/Admin/GetStudentById",
        data: {
            studentId: parseInt(id)
        }, success: callback
    })
}

function getQuestionById(id, callback) {
    $.ajax({
        type: "get",
        url: "/Admin/GetQuestionById",
        data: {
            questionId: parseInt(id)
        }, success: callback
    })
}

function getTeacherById(id, callback) {
    $.ajax({
        type: "get",
        url: "/Admin/GetTeacherById",
        data: {
            teacherId: parseInt(id)
        }, success: callback
    })
}

function errorAlert(message) {
    Swal.fire({
        icon: 'error',
        title: 'Oops...',
        text: message
    })
}

function warningAlert(title, message) {
    Swal.fire({
        title: title,
        text: message,
        icon: 'warning'
    })
}

function confirmAlert(message, callback) {
    Swal.fire({
        title: message,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Xác nhận',
        cancelButtonText: "Hủy bỏ",
        preConfirm: callback
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.close();
        }
    })
}

function formatQuestionType(type) {
    let result = "";
    if (type == 1) {
        result = "Câu hỏi nhiều câu trả lời";
    } else if (type == 2) {
        result = "Câu hỏi nối";
    } else if (type == 3) {
        result = "Câu hỏi điền từ";
    } else if (type == 4) {
        result = "Câu hỏi viết lại câu";
    } else if (type == 5) {
        result = "Câu hỏi tự luận";
    } else if (type == 6) {
        result = "Câu hỏi đúng / sai";
    } else {
        result = "Không xác định";
    }
    return result;
}

function formatData(data) {
    let result = "";
    result = data.replace(/<audio[^>]*>(.*?)audio>/g, "[âm thanh]");
    result = result.replace(/<video[^>]*>(.*?)video>/g, "[video]");
    result = result.replace(/<img[^>]*>/g, "[hình ảnh]");
    result = result.replace(/<a[^>]*>(.*?)a>/g, "[tệp tin]");
    result = result.replace(/\[(\S+)\]/g, "...............");
    return result;
}

function uploadDocCallBack(file, elementId, classIdentifier) {
    let fileData = new FormData();
    fileData.append(file.name, file);
    $.ajax({
        type: "POST",
        url: "/Admin/UploadDoc?Filename=", //Your own back-end uploader
        contentType: false,
        processData: false,
        data: fileData,
        async: true,
        success: function (response) {
            const jsonResponse = JSON.parse(response);

            //updateProgressById("progressCreateQuestionTextEdit", 0);
            let listMimeImg = ['image/png', 'image/jpeg', 'image/webp', 'image/gif', 'image/svg'];
            let listMimeAudio = ['audio/mpeg', 'audio/ogg'];
            let listMimeVideo = ['video/mpeg', 'video/mp4', 'video/webm', 'video/quicktime'];
            let elem;
            if (listMimeImg.indexOf(file.type) > -1) {
                //Picture
                elem = document.createElement("img");
                elem.setAttribute("src", "/Admin/StreamFile/" + jsonResponse.DocId);
                elem.setAttribute("preload", "metadata");
                elem.setAttribute("height", "auto");
                elem.setAttribute("width", "auto");
                elem.classList.add(classIdentifier);
                elem.classList.add("col-12");
                $(elementId).summernote('editor.insertNode', elem);
            } else if (listMimeAudio.indexOf(file.type) > -1) {
                //Audio
                elem = document.createElement("audio");
                elem.setAttribute("src", "/Admin/StreamFile/" + jsonResponse.DocId);
                elem.setAttribute("controls", "controls");
                elem.setAttribute("preload", "metadata");
                elem.classList.add("col-12");
                elem.classList.add(classIdentifier);
                $(elementId).summernote('editor.insertNode', elem);
            } else if (listMimeVideo.indexOf(file.type) > -1) {
                //Video
                elem = document.createElement("video");
                elem.setAttribute("src", "/Admin/StreamFile/" + jsonResponse.DocId);
                elem.setAttribute("controls", "controls");
                elem.setAttribute("preload", "metadata");
                elem.setAttribute("type", "video/mp4");
                elem.classList.add("col-12");
                elem.classList.add(classIdentifier);
                $(elementId).summernote('editor.insertNode', elem);
            } else {
                //Other file type
                elem = document.createElement("a");
                let linkText = document.createTextNode(file.name);
                elem.appendChild(linkText);
                elem.title = file.name;
                elem.href = "/Admin/File/" + jsonResponse.DocId;
                elem.classList.add("col-12");
                elem.classList.add(classIdentifier);
                $(elementId).summernote('editor.insertNode', elem);
            }
        },
        xhr: function () {
            let xhr = new window.XMLHttpRequest();
            xhr.upload.addEventListener("progress", function (evt) {
                if (evt.lengthComputable) {
                    let percentComplete = Math.round((evt.loaded / evt.total) * 100);
                    if (percentComplete < 98) {
                        //updateProgressById("progressCreateQuestionTextEdit", percentComplete);
                    }
                }
            }, false);
            return xhr;
        },
        error: () => {
            toastr.error("Lỗi tải lên file!");
        }
    });
}

function dateLocalParse(dateString) {
    let date = new Date(dateString).toJSON().slice(0, 11);
    let time = new Date(dateString).toString().slice(16, -29);
    return date + time;
}

function checkValue(obj) {
    return obj && obj !== 'null' && obj !== 'undefined';
}

function increaceInputValue(element, value) {
    var currentValue = parseInt($(element).val());
    var newValue = currentValue + value;
    $(element).val(newValue);
}