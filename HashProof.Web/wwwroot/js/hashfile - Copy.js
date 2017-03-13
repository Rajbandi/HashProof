$().ready(function() {
    getUnique = function() {
        var uniquecnt = 0;

        function getUnique() {
            return (uniquecnt++);
        }

        return getUnique;
    }();

    function decimalToHexString(number) {
        if (number < 0) {
            number = 0xFFFFFFFF + number + 1;
        }
        return number.toString(16);
    }

    function digits(number, dig) {
        var shift = Math.pow(10, dig);
        return Math.floor(number * shift) / shift;
    }

    function swapendian32(val) {
        return (((val & 0xFF) << 24) | ((val & 0xFF00) << 8) | ((val >> 8) & 0xFF00) | ((val >> 24) & 0xFF)) >>> 0;
    }

    function arrayBufferToWordArray(arrayBuffer) {
        var fullWords = Math.floor(arrayBuffer.byteLength / 4);
        var bytesLeft = arrayBuffer.byteLength % 4;
        var u32 = new Uint32Array(arrayBuffer, 0, fullWords);
        var u8 = new Uint8Array(arrayBuffer);
        var cp = [];
        for (var i = 0; i < fullWords; ++i) {
            cp.push(swapendian32(u32[i]));
        }
        if (bytesLeft) {
            var pad = 0;
            for (var i = bytesLeft; i > 0; --i) {
                pad = pad << 8;
                pad += u8[u8.byteLength - i];
            }
            for (var i = 0; i < 4 - bytesLeft; ++i) {
                pad = pad << 8;
            }
            cp.push(pad);
        }
        return CryptoJS.lib.WordArray.create(cp, arrayBuffer.byteLength);
    };

    function bytes2si(bytes, outputdigits) {
        if (bytes < 1024) {
            return digits(bytes, outputdigits) + " b";
        } else if (bytes < 1048576) {
            return digits(bytes / 1024, outputdigits) + " KiB";
        }
        return digits(bytes / 1048576, outputdigits) + " MiB";
    }

    function bytes2si2(bytes1, bytes2, outputdigits) {
        var big = Math.max(bytes1, bytes2);
        if (big < 1024) {
            return bytes1 + "/" + bytes2 + " b";
        } else if (big < 1048576) {
            return digits(bytes1 / 1024, outputdigits) +
                "/" +
                digits(bytes2 / 1024, outputdigits) +
                " KiB";
        }
        return digits(bytes1 / 1048576, outputdigits) +
            "/" +
            digits(bytes2 / 1048576, outputdigits) +
            " MiB";
    }

    function progressiveRead(file, work, done) {
        var chunkSize = 204800;
        var pos = 0;
        var reader = new FileReader();

        function progressiveReadNext() {
            var end = Math.min(pos + chunkSize, file.size);
            reader.onload = function(e) {
                pos = end;
                work(e.target.result, pos, file);
                if (pos < file.size) {
                    setTimeout(progressiveReadNext, 0);
                } else {
                    done(file);
                }
            }
            if (file.slice) {
                var blob = file.slice(pos, end);
            } else if (file.webkitSlice) {
                var blob = file.webkitSlice(pos, end);
            }
            reader.readAsArrayBuffer(blob);
        }

        setTimeout(progressiveReadNext, 0);
    };

    function selectFile(f) {
        (function() {
            var start = (new Date).getTime();
            var lastprogress = 0;
            var sha256 = CryptoJS.algo.SHA256.create();
            var uid = "filehash" + getUnique();
            // var newrow = '<tr><td colspan="2" class="red hash_file_info" id="' + uid + '"></td></tr>';
            progressiveRead(f,
                function(data, pos, file) {
                    var wordArray = arrayBufferToWordArray(data);
                    sha256.update(wordArray);

                    var progress = Math.floor((pos / file.size) * 100);
                    if (progress > lastprogress) {
                        $(file.previewElement).find('.dz-progress .dz-upload').css('width', progress + '%');
                        var took = ((new Date).getTime() - start) / 1000;
                        // $('#' + uid).html(file.name + '（' + bytes2si2(pos, file.size, 2) + '）| Time: ' + digits(took, 2) + 's @ ' + bytes2si(pos / took, 2) + '/s ')
                        lastprogress = progress;
                    }
                },
                function(file) {
                    $(file.previewElement).removeClass('dz-progressing');
                    $(file.previewElement).addClass('dz-success dz-complete');
                    var took = ((new Date).getTime() - start) / 1000;

                    var hash = sha256.finalize();
                    if(showDocHash)
                         showDocHash(hash);
                    else {
                        console.log(hash);
                    }
                    //  $("#" + uid).parent('tr').after(results);
                });
        })();
    }

    function compatible() {
        try {
            if (typeof FileReader == "undefined") return false;
            if (typeof Blob == "undefined") return false;
            var blob = new Blob();
            if (!blob.slice && !blob.webkitSlice) return false;
            if (!('draggable' in document.createElement('span'))) return false;
        } catch (e) {
            return false;
        }
        return true;
    }

    if (!compatible()) {
        alert('Sorry, please use a HTML5 browser!');
    }

    Dropzone.options.hashdrop = {
        autoProcessQueue: false,
        maxFiles: 1,
        maxFilesize:2048,
        init: function() {

            this.on("maxfilesexceeded",
                function(file) {
                    this.removeAllFiles();
                    this.addFile(file);
                });
            this.on("addedfile",
                function (file) {
                   
                    selectFile(file);
                  
                });
        }
    };
//var hashFile = new Dropzone("#hash_file");
//hashFile.options.maxFilesize = 1;
//hashFile.options.autoProcessQueue = false;
//hashFile.on("addedfile", function (file) {

//});

});