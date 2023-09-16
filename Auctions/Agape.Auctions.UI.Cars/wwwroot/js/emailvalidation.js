function emailvalidation(email, callback) {
    var emailReg = /^([a-zA-Z0-9_.+-])+\@(([a-zA-Z0-9-])+\.)+([a-zA-Z0-9]{2,4})+$/;
    if (!emailReg.test(email)) {
        callback('0');
    } else {
        callback('1');
    }
}