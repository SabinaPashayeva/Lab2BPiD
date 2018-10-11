$(function () {

    $('#chatBody').hide();
    $('#loginBlock').show();
    var chat = $.connection.chatHub;
    var crypt = new JSEncrypt({ default_key_size: 2048 });
    var serverPublicKey;
    var e, n;
    
    //Getting message
    chat.client.addMessage = function (name, message) {
        message = crypt.decrypt(message);
        
        $('#chatroom').append('<p><b>' + htmlEncode(name)
            + '</b>: ' + htmlEncode(message) + '</p>');
    };

    chat.client.sendPublicKey = function (message) {
        serverPublicKey = jQuery.parseJSON(message);
    }
    
    chat.client.onConnected = function (id, userName, allUsers) {

        $('#loginBlock').hide();
        $('#chatBody').show();
        $('#hdId').val(id);
        $('#username').val(userName);
        $('#header').html('<h3 style="text-align: center;color: #555555;">Welcome ' + userName + '! </h3>');

        for (i = 0; i < allUsers.length; i++) {

            AddUser(allUsers[i].ConnectionId, allUsers[i].Name);
        }
    }

    chat.client.onNewUserConnected = function (id, name) {

        AddUser(id, name);
    }

    chat.client.onUserDisconnected = function (id, userName) {

        $('#' + id).remove();
    }

    // Open connection
    $.connection.hub.start().done(function () {

     //   chat.server.sendWelcomeMessage();
        $('#sendmessage').click(function () {
            //Encrypting message, sending to server
            var k = new RSAKey();
            k.setPublic(base64ToHex(serverPublicKey[1]), "010001");
            var k1 = k.encrypt($('#message').val());

            chat.server.send($('#username').val(), hexToBase64(k1));
            $('#message').val('');
        });

        // login processing
        $("#btnLogin").click(function () {

            var name = $("#txtUserName").val();
            if (name.length > 0) {
                chat.server.connect(name);
            }
            else {
                alert("Enter login!");
            }
            //generating public keys
            crypt.getKey();
            var keys = generateKeys(crypt.key);
            e = keys[0];
            n = keys[1];
            chat.server.getPublicKey(e, n);
        });
    });
});

function htmlEncode(value) {
    var encodedValue = $('<div />').text(value).html();
    return encodedValue;
}

function AddUser(id, name) {

    var userId = $('#hdId').val();

    if (userId !== id) {

        $("#chatusers").append('<p id="' + id + '"><b>' + name + '</b></p>');
    }
}

function generateKeys(key) {
    e = key.e.toString(16);
    n = key.n.toString(16);
    var tmpArray = [hexToBase64(e), hexToBase64(n)];
    return tmpArray;
}

//HEX to Base64, Base64 to HEX
if (!window.atob) {
    var tableStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    var table = tableStr.split("");

    window.atob = function (base64) {
        if (/(=[^=]+|={3,})$/.test(base64)) throw new Error("String contains an invalid character");
        base64 = base64.replace(/=/g, "");
        var n = base64.length & 3;
        if (n === 1) throw new Error("String contains an invalid character");
        for (var i = 0, j = 0, len = base64.length / 4, bin = []; i < len; ++i) {
            var a = tableStr.indexOf(base64[j++] || "A"), b = tableStr.indexOf(base64[j++] || "A");
            var c = tableStr.indexOf(base64[j++] || "A"), d = tableStr.indexOf(base64[j++] || "A");
            if ((a | b | c | d) < 0) throw new Error("String contains an invalid character");
            bin[bin.length] = ((a << 2) | (b >> 4)) & 255;
            bin[bin.length] = ((b << 4) | (c >> 2)) & 255;
            bin[bin.length] = ((c << 6) | d) & 255;
        };
        return String.fromCharCode.apply(null, bin).substr(0, bin.length + n - 4);
    };

    window.btoa = function (bin) {
        for (var i = 0, j = 0, len = bin.length / 3, base64 = []; i < len; ++i) {
            var a = bin.charCodeAt(j++), b = bin.charCodeAt(j++), c = bin.charCodeAt(j++);
            if ((a | b | c) > 255) throw new Error("String contains an invalid character");
            base64[base64.length] = table[a >> 2] + table[((a << 4) & 63) | (b >> 4)] +
                (isNaN(b) ? "=" : table[((b << 2) & 63) | (c >> 6)]) +
                (isNaN(b + c) ? "=" : table[c & 63]);
        }
        return base64.join("");
    };

}

function hexToBase64(str) {
    return btoa(String.fromCharCode.apply(null,
        str.replace(/\r|\n/g, "").replace(/([\da-fA-F]{2}) ?/g, "0x$1 ").replace(/ +$/, "").split(" "))
    );
}

function base64ToHex(str) {
    for (var i = 0, bin = atob(str.replace(/[ \r\n]+$/, "")), hex = []; i < bin.length; ++i) {
        var tmp = bin.charCodeAt(i).toString(16);
        if (tmp.length === 1) tmp = "0" + tmp;
        hex[hex.length] = tmp;
    }
    return hex.join(" ");
}
