//

// Novi Builder Variables
var isNoviBuilder = window.xMode;

/*----------------------------------------------------*/
/* MOBILE DETECT FUNCTION
/*----------------------------------------------------*/
var isMobile = {
  Android: function () {
    return navigator.userAgent.match(/Android/i);
  },
  BlackBerry: function () {
    return navigator.userAgent.match(/BlackBerry/i);
  },
  iOS: function () {
    return navigator.userAgent.match(/iPhone|iPad|iPod/i);
  },
  Opera: function () {
    return navigator.userAgent.match(/Opera Mini/i);
  },
  Windows: function () {
    return navigator.userAgent.match(/IEMobile/i);
  },
  any: function () {
    return (isMobile.Android() || isMobile.BlackBerry() || isMobile.iOS() || isMobile.Opera() || isMobile.Windows());
  }
};


/////////////////////// ready
$(document).ready(function () {
  var userAgent = navigator.userAgent.toLowerCase(),
    initialDate = new Date(),
    $window = $(window),
    isIE = userAgent.indexOf("msie") !== -1 ? parseInt(userAgent.split("msie")[1], 10) : userAgent.indexOf("trident") !== -1 ? 11 : userAgent.indexOf("edge") !== -1 ? 12 : false,
    isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent),
    gMapAPILoaded = false,

    plugins = {
      pointerEvents: isIE < 11 ? "js/pointer-events.min.js" : false,
      bootstrapTabs: $(".tabs-custom-init"),
      materialParallax: $(".parallax-container"),
      maps: $(".google-map-container"),
      swiper: $(".swiper-slider"),
      superfish: $('.sf-menu'),
      rdMailForm: $(".rd-mailform"),
      rdInputLabel: $(".form-label"),
      rdNavbar: $(".rd-navbar"),
      regula: $("[data-constraints]"),
      radio: $("input[type='radio']"),
      checkbox: $("input[type='checkbox']"),
      captcha: $('.recaptcha'),
      owl: $(".owl-carousel"),
      lightGallery: $("[data-lightgallery='group']"),
      lightGalleryItem: $("[data-lightgallery='item']"),
      lightDynamicGalleryItem: $("[data-lightgallery='dynamic']"),
      customWaypoints: $('[data-custom-scroll-to]'),
      copyrightYear: $(".copyright-year")
    };

  /**
   * @desc Check the element was been scrolled into the view
   * @param {object} elem - jQuery object
   * @return {boolean}
   */
  function isScrolledIntoView ( elem ) {
    if ( isNoviBuilder ) return true;
    return elem.offset().top + elem.outerHeight() >= $window.scrollTop() && elem.offset().top <= $window.scrollTop() + $window.height();
  }

  /**
   * @desc Calls a function when element has been scrolled into the view
   * @param {object} element - jQuery object
   * @param {function} func - init function
   */
  function lazyInit( element, func ) {
    var scrollHandler = function () {
      if ( ( !element.hasClass( 'lazy-loaded' ) && ( isScrolledIntoView( element ) ) ) ) {
        func.call();
        element.addClass( 'lazy-loaded' );
      }
    };

    scrollHandler();
    $window.on( 'scroll', scrollHandler );
  }

  /*----------------------------------------------------*/
  // IE Polyfills
  /*----------------------------------------------------*/
  if (isIE) {
    if (isIE < 10) {
      $html.addClass("lt-ie-10");
    }

    if (isIE < 11) {
      if (plugins.pointerEvents) {
        $.getScript(plugins.pointerEvents)
          .done(function () {
            $html.addClass("ie-10");
            PointerEventsPolyfill.initialize({});
          });
      }
    }

    if (isIE === 11) {
      $("html").addClass("ie-11");
    }

    if (isIE === 12) {
      $("html").addClass("ie-edge");
    }
  }

  /*----------------------------------------------------*/
  // PARALLAX CALLING
  /*----------------------------------------------------*/
  if (plugins.materialParallax.length) {
    if (!isNoviBuilder && !isIE && !isMobile) {
      plugins.materialParallax.parallax();

      // heavy pages fix
      $window.on('load', function () {
        setTimeout(function () {
          $window.scroll();
        }, 500);
      });
    } else {
      for (var i = 0; i < plugins.materialParallax.length; i++) {
        var parallax = $(plugins.materialParallax[i]),
          imgPath = parallax.data("parallax-img");

        parallax.css({
          "background-image": 'url(' + imgPath + ')',
          "background-size": "cover"
        });
      }
    }
  }

  /*----------------------------------------------------*/
  // Bootstrap tabs
  /*----------------------------------------------------*/
  if (plugins.bootstrapTabs.length) {
    var i;
    for (i = 0; i < plugins.bootstrapTabs.length; i++) {
      var bootstrapTabsItem = $(plugins.bootstrapTabs[i]);
    }
  }


  /**
   * @desc Google map function for getting latitude and longitude
   */
  function getLatLngObject(str, marker, map, callback) {
    var coordinates = {};
    try {
      coordinates = JSON.parse(str);
      callback(new google.maps.LatLng(
        coordinates.lat,
        coordinates.lng
      ), marker, map)
    } catch (e) {
      map.geocoder.geocode({'address': str}, function (results, status) {
        if (status === google.maps.GeocoderStatus.OK) {
          var latitude = results[0].geometry.location.lat();
          var longitude = results[0].geometry.location.lng();

          callback(new google.maps.LatLng(
            parseFloat(latitude),
            parseFloat(longitude)
          ), marker, map)
        }
      })
    }
  }

  /**
   * @desc Initialize Google maps
   */
  function initMaps() {
    var key;

    for ( var i = 0; i < plugins.maps.length; i++ ) {
      if ( plugins.maps[i].hasAttribute( "data-key" ) ) {
        key = plugins.maps[i].getAttribute( "data-key" );
        break;
      }
    }

    $.getScript('//maps.google.com/maps/api/js?'+ ( key ? 'key='+ key + '&' : '' ) +'sensor=false&libraries=geometry,places&v=quarterly', function () {
      var head = document.getElementsByTagName('head')[0],
        insertBefore = head.insertBefore;

      head.insertBefore = function (newElement, referenceElement) {
        if (newElement.href && newElement.href.indexOf('//fonts.googleapis.com/css?family=Roboto') !== -1 || newElement.innerHTML.indexOf('gm-style') !== -1) {
          return;
        }
        insertBefore.call(head, newElement, referenceElement);
      };
      var geocoder = new google.maps.Geocoder;
      for (var i = 0; i < plugins.maps.length; i++) {
        var zoom = parseInt(plugins.maps[i].getAttribute("data-zoom"), 10) || 11;
        var styles = plugins.maps[i].hasAttribute('data-styles') ? JSON.parse(plugins.maps[i].getAttribute("data-styles")) : [];
        var center = plugins.maps[i].getAttribute("data-center") || "New York";

        // Initialize map
        var map = new google.maps.Map(plugins.maps[i].querySelectorAll(".google-map")[0], {
          zoom: zoom,
          styles: styles,
          scrollwheel: false,
          center: {lat: 0, lng: 0}
        });

        // Add map object to map node
        plugins.maps[i].map = map;
        plugins.maps[i].geocoder = geocoder;
        plugins.maps[i].keySupported = true;
        plugins.maps[i].google = google;

        // Get Center coordinates from attribute
        getLatLngObject(center, null, plugins.maps[i], function (location, markerElement, mapElement) {
          mapElement.map.setCenter(location);
        });

        // Add markers from google-map-markers array
        var markerItems = plugins.maps[i].querySelectorAll(".google-map-markers li");

        if (markerItems.length){
          var markers = [];
          for (var j = 0; j < markerItems.length; j++){
            var markerElement = markerItems[j];
            getLatLngObject(markerElement.getAttribute("data-location"), markerElement, plugins.maps[i], function(location, markerElement, mapElement){
              var icon = markerElement.getAttribute("data-icon") || mapElement.getAttribute("data-icon");
              var activeIcon = markerElement.getAttribute("data-icon-active") || mapElement.getAttribute("data-icon-active");
              var info = markerElement.getAttribute("data-description") || "";
              var infoWindow = new google.maps.InfoWindow({
                content: info
              });
              markerElement.infoWindow = infoWindow;
              var markerData = {
                position: location,
                map: mapElement.map
              }
              if (icon){
                markerData.icon = icon;
              }
              var marker = new google.maps.Marker(markerData);
              markerElement.gmarker = marker;
              markers.push({markerElement: markerElement, infoWindow: infoWindow});
              marker.isActive = false;
              // Handle infoWindow close click
              google.maps.event.addListener(infoWindow,'closeclick',(function(markerElement, mapElement){
                var markerIcon = null;
                markerElement.gmarker.isActive = false;
                markerIcon = markerElement.getAttribute("data-icon") || mapElement.getAttribute("data-icon");
                markerElement.gmarker.setIcon(markerIcon);
              }).bind(this, markerElement, mapElement));


              // Set marker active on Click and open infoWindow
              google.maps.event.addListener(marker, 'click', (function(markerElement, mapElement) {
                if (markerElement.infoWindow.getContent().length === 0) return;
                var gMarker, currentMarker = markerElement.gmarker, currentInfoWindow;
                for (var k =0; k < markers.length; k++){
                  var markerIcon;
                  if (markers[k].markerElement === markerElement){
                    currentInfoWindow = markers[k].infoWindow;
                  }
                  gMarker = markers[k].markerElement.gmarker;
                  if (gMarker.isActive && markers[k].markerElement !== markerElement){
                    gMarker.isActive = false;
                    markerIcon = markers[k].markerElement.getAttribute("data-icon") || mapElement.getAttribute("data-icon")
                    gMarker.setIcon(markerIcon);
                    markers[k].infoWindow.close();
                  }
                }

                currentMarker.isActive = !currentMarker.isActive;
                if (currentMarker.isActive) {
                  if (markerIcon = markerElement.getAttribute("data-icon-active") || mapElement.getAttribute("data-icon-active")){
                    currentMarker.setIcon(markerIcon);
                  }

                  currentInfoWindow.open(map, marker);
                }else{
                  if (markerIcon = markerElement.getAttribute("data-icon") || mapElement.getAttribute("data-icon")){
                    currentMarker.setIcon(markerIcon);
                  }
                  currentInfoWindow.close();
                }
              }).bind(this, markerElement, mapElement))
            })
          }
        }
      }
    });
  }


  // Google maps
  if( plugins.maps.length ) {
    lazyInit( plugins.maps, initMaps );
  }


  /*----------------------------------------------------*/
  // RD Mailform
  /*----------------------------------------------------*/

  // attachFormValidator
  function attachFormValidator(elements) {
    for (var i = 0; i < elements.length; i++) {
      var o = $(elements[i]), v;
      o.addClass("form-control-has-validation").after("<span class='form-validation'></span>");
      v = o.parent().find(".form-validation");
      if (v.is(":last-child")) {
        o.addClass("form-control-last-child");
      }
    }

    elements
      .on('input change propertychange blur', function (e) {
        var $this = $(this), results;

        if (e.type !== "blur") {
          if (!$this.parent().hasClass("has-error")) {
            return;
          }
        }

        if ($this.parents('.rd-mailform').hasClass('success')) {
          return;
        }

        if ((results = $this.regula('validate')).length) {
          for (i = 0; i < results.length; i++) {
            $this.siblings(".form-validation").text(results[i].message).parent().addClass("has-error")
          }
        } else {
          $this.siblings(".form-validation").text("").parent().removeClass("has-error")
        }
      })
      .regula('bind');

    var regularConstraintsMessages = [
      {
        type: regula.Constraint.Required,
        newMessage: "The text field is required."
      },
      {
        type: regula.Constraint.Email,
        newMessage: "The email is not a valid email."
      },
      {
        type: regula.Constraint.Numeric,
        newMessage: "Only numbers are required"
      },
      {
        type: regula.Constraint.Selected,
        newMessage: "Please choose an option."
      }
    ];


    for (var i = 0; i < regularConstraintsMessages.length; i++) {
      var regularConstraint = regularConstraintsMessages[i];

      regula.override({
        constraintType: regularConstraint.type,
        defaultMessage: regularConstraint.newMessage
      });
    }
  }

  // isValidated
  function isValidated(elements, captcha) {
    var results, errors = 0;

    if (elements.length) {
      for (j = 0; j < elements.length; j++) {

        var $input = $(elements[j]);
        if ((results = $input.regula('validate')).length) {
          for (k = 0; k < results.length; k++) {
            errors++;
            $input.siblings(".form-validation").text(results[k].message).parent().addClass("has-error");
          }
        } else {
          $input.siblings(".form-validation").text("").parent().removeClass("has-error")
        }
      }

      if (captcha) {
        if (captcha.length) {
          return validateReCaptcha(captcha) && errors === 0
        }
      }

      return errors === 0;
    }
    return true;
  }

  // validateReCaptcha
  function validateReCaptcha(captcha) {
    var captchaToken = captcha.find('.g-recaptcha-response').val();

    if (captchaToken.length === 0) {
      captcha
        .siblings('.form-validation')
        .html('Please, prove that you are not robot.')
        .addClass('active');
      captcha
        .closest('.form-group')
        .addClass('has-error');

      captcha.on('propertychange', function () {
        var $this = $(this),
          captchaToken = $this.find('.g-recaptcha-response').val();

        if (captchaToken.length > 0) {
          $this
            .closest('.form-group')
            .removeClass('has-error');
          $this
            .siblings('.form-validation')
            .removeClass('active')
            .html('');
          $this.off('propertychange');
        }
      });

      return false;
    }

    return true;
  }

  // onloadCaptchaCallback
  window.onloadCaptchaCallback = function () {
    for (i = 0; i < plugins.captcha.length; i++) {
      var $capthcaItem = $(plugins.captcha[i]);

      grecaptcha.render(
        $capthcaItem.attr('id'),
        {
          sitekey: $capthcaItem.attr('data-sitekey'),
          size: $capthcaItem.attr('data-size') ? $capthcaItem.attr('data-size') : 'normal',
          theme: $capthcaItem.attr('data-theme') ? $capthcaItem.attr('data-theme') : 'light',
          callback: function (e) {
            $('.recaptcha').trigger('propertychange');
          }
        }
      );
      $capthcaItem.after("<span class='form-validation'></span>");
    }
  };

  // Google ReCaptcha
  if (plugins.captcha.length) {
    $.getScript("//www.google.com/recaptcha/api.js?onload=onloadCaptchaCallback&render=explicit&hl=en");
  }

  // Radio
  if (plugins.radio.length) {
    var i;
    for (i = 0; i < plugins.radio.length; i++) {
      $(plugins.radio[i]).addClass("radio-custom").after("<span class='radio-custom-dummy'></span>")
    }
  }

  // Checkbox
  if (plugins.checkbox.length) {
    var i;
    for (i = 0; i < plugins.checkbox.length; i++) {
      $(plugins.checkbox[i]).addClass("checkbox-custom").after("<span class='checkbox-custom-dummy'></span>")
    }
  }

  // RD Input Label
  if (plugins.rdInputLabel.length) {
    plugins.rdInputLabel.RDInputLabel();
  }

  // Regula
  if (plugins.regula.length) {
    attachFormValidator(plugins.regula);
  }

  // RD Mailform
  if (plugins.rdMailForm.length) {
    var i, j, k,
      msg = {
        'MF000': 'Successfully sent!',
        'MF001': 'Recipients are not set!',
        'MF002': 'Form will not work locally!',
        'MF003': 'Please, define email field in your form!',
        'MF004': 'Please, define type of your form!',
        'MF254': 'Something went wrong with PHPMailer!',
        'MF255': 'Aw, snap! Something went wrong.'
      };

    for (i = 0; i < plugins.rdMailForm.length; i++) {
      var $form = $(plugins.rdMailForm[i]),
        formHasCaptcha = false;

      $form.attr('novalidate', 'novalidate').ajaxForm({
        data: {
          "form-type": $form.attr("data-form-type") || "contact",
          "counter": i
        },
        beforeSubmit: function (arr, $form, options) {
          if (isNoviBuilder)
            return;

          var form = $(plugins.rdMailForm[this.extraData.counter]),
            inputs = form.find("[data-constraints]"),
            output = $("#" + form.attr("data-form-output")),
            captcha = form.find('.recaptcha'),
            captchaFlag = true;

          output.removeClass("active error success");

          if (isValidated(inputs, captcha)) {

            // veify reCaptcha
            if (captcha.length) {
              var captchaToken = captcha.find('.g-recaptcha-response').val(),
                captchaMsg = {
                  'CPT001': 'Please, setup you "site key" and "secret key" of reCaptcha',
                  'CPT002': 'Something wrong with google reCaptcha'
                };

              formHasCaptcha = true;

              $.ajax({
                method: "POST",
                url: "bat/reCaptcha.php",
                data: {'g-recaptcha-response': captchaToken},
                async: false
              })
                .done(function (responceCode) {
                  if (responceCode !== 'CPT000') {
                    if (output.hasClass("snackbars")) {
                      output.html('<p><span class="icon text-middle fa fa-check icon-xxs"></span><span>' + captchaMsg[responceCode] + '</span></p>')

                      setTimeout(function () {
                        output.removeClass("active");
                      }, 3500);

                      captchaFlag = false;
                    } else {
                      output.html(captchaMsg[responceCode]);
                    }

                    output.addClass("active");
                  }
                });
            }

            if (!captchaFlag) {
              return false;
            }

            form.addClass('form-in-process');

            if (output.hasClass("snackbars")) {
              output.html('<p><span class="icon text-middle fa fa-circle-o-notch fa-spin icon-xxs"></span><span>Sending</span></p>');
              output.addClass("active");
            }
          } else {
            return false;
          }
        },
        error: function (result) {
          if (isNoviBuilder)
            return;

          var output = $("#" + $(plugins.rdMailForm[this.extraData.counter]).attr("data-form-output")),
            form = $(plugins.rdMailForm[this.extraData.counter]);

          output.text(msg[result]);
          form.removeClass('form-in-process');

          if (formHasCaptcha) {
            grecaptcha.reset();
          }
        },
        success: function (result) {
          if (isNoviBuilder)
            return;

          var form = $(plugins.rdMailForm[this.extraData.counter]),
            output = $("#" + form.attr("data-form-output")),
            select = form.find('select');

          form
            .addClass('success')
            .removeClass('form-in-process');

          if (formHasCaptcha) {
            grecaptcha.reset();
          }

          result = result.length === 5 ? result : 'MF255';
          output.text(msg[result]);

          if (result === "MF000") {
            if (output.hasClass("snackbars")) {
              output.html('<p><span class="icon text-middle fa fa-check icon-xxs"></span><span>' + msg[result] + '</span></p>');
            } else {
              output.addClass("active success");
            }
          } else {
            if (output.hasClass("snackbars")) {
              output.html(' <p class="snackbars-left"><span class="icon icon-xxs fa fa-exclamation-triangle text-middle"></span><span>' + msg[result] + '</span></p>');
            } else {
              output.addClass("active error");
            }
          }

          form.clearForm();

          if (select.length) {
            select.select2("val", "");
          }

          form.find('input, textarea').trigger('blur');

          setTimeout(function () {
            output.removeClass("active error success");
            form.removeClass('success');
          }, 3500);
        }
      });
    }
  }

  /**
   * Custom Waypoints
   */
  if (plugins.customWaypoints.length && !isNoviBuilder) {
    var i;
    for (i = 0; i < plugins.customWaypoints.length; i++) {
      var $this = $(plugins.customWaypoints[i]);

      $this.on('click', function (e) {
        e.preventDefault();

        $("body, html").stop().animate({
          scrollTop: $("#" + $(this).attr('data-custom-scroll-to')).offset().top
        }, 1000, function () {
          $window.trigger("resize");
        });
      });
    }
  }

  // RD Navbar
  if (plugins.rdNavbar.length) {
    var aliaces, i, j, len, value, values, responsiveNavbar;

    aliaces = ["-", "-sm-", "-md-", "-lg-", "-xl-", "-xxl-"];
    values = [0, 576, 768, 992, 1200, 1600];
    responsiveNavbar = {};

    for (i = j = 0, len = values.length; j < len; i = ++j) {
      value = values[i];
      if (!responsiveNavbar[values[i]]) {
        responsiveNavbar[values[i]] = {};
      }
      if (plugins.rdNavbar.attr('data' + aliaces[i] + 'layout')) {
        responsiveNavbar[values[i]].layout = plugins.rdNavbar.attr('data' + aliaces[i] + 'layout');
      }
      if (plugins.rdNavbar.attr('data' + aliaces[i] + 'device-layout')) {
        responsiveNavbar[values[i]]['deviceLayout'] = plugins.rdNavbar.attr('data' + aliaces[i] + 'device-layout');
      }
      if (plugins.rdNavbar.attr('data' + aliaces[i] + 'hover-on')) {
        responsiveNavbar[values[i]]['focusOnHover'] = plugins.rdNavbar.attr('data' + aliaces[i] + 'hover-on') === 'true';
      }
      if (plugins.rdNavbar.attr('data' + aliaces[i] + 'auto-height')) {
        responsiveNavbar[values[i]]['autoHeight'] = plugins.rdNavbar.attr('data' + aliaces[i] + 'auto-height') === 'true';
      }

      if (isNoviBuilder) {
        responsiveNavbar[values[i]]['stickUp'] = false;
      } else if (plugins.rdNavbar.attr('data' + aliaces[i] + 'stick-up')) {
        responsiveNavbar[values[i]]['stickUp'] = plugins.rdNavbar.attr('data' + aliaces[i] + 'stick-up') === 'true';
      }

      if (plugins.rdNavbar.attr('data' + aliaces[i] + 'stick-up-offset')) {
        responsiveNavbar[values[i]]['stickUpOffset'] = plugins.rdNavbar.attr('data' + aliaces[i] + 'stick-up-offset');
      }
    }

    plugins.rdNavbar.RDNavbar({
      anchorNav: !isNoviBuilder,
      stickUpClone: (plugins.rdNavbar.attr("data-stick-up-clone") && !isNoviBuilder) ? plugins.rdNavbar.attr("data-stick-up-clone") === 'true' : false,
      responsive: responsiveNavbar,
      callbacks: {
        onStuck: function () {
          var navbarSearch = this.$element.find('.rd-search input');

          if (navbarSearch) {
            navbarSearch.val('').trigger('propertychange');
          }
        },
        onDropdownOver: function () {
          return !isNoviBuilder;
        },
        onUnstuck: function () {
          if (this.$clone === null)
            return;
        }
      }
    });

    if (plugins.rdNavbar.attr("data-body-class")) {
      document.body.className += ' ' + plugins.rdNavbar.attr("data-body-class");
    }
  }

  /*----------------------------------------------------*/
  // carouFredSel.
  /*----------------------------------------------------*/

  var o = $('#review .carousel.main ul');
  if (o.length > 0) {
    o.carouFredSel({
      auto: {
        play: !isNoviBuilder ? true : false,
        timeoutDuration: 8000
      },
      responsive: true,
      pagination: '.review_pagination',
      width: '100%',
      scroll: {
        // fx : "crossfade",
        items: 1,
        duration: 1000,
        easing: "easeOutExpo"
      },
      items: {
        width: '600',
        height: 'variable', //  optionally resize item-height
        visible: {
          min: 1,
          max: 1
        }
      },
      mousewheel: false,
      swipe: {
        onMouse: !isNoviBuilder ? true : false,
        onTouch: true
      }
    });
  }
  ;

  $(window).bind("resize", updateSizes_vat).bind("load", updateSizes_vat);

  function updateSizes_vat() {

    $('#review .carousel.main ul').trigger("updateSizes");

  }

  updateSizes_vat();

  /*----------------------------------------------------*/
  // Sticky.
  /*----------------------------------------------------*/
  if (!isNoviBuilder) {
    $("#top2").sticky({
      topSpacing: 0,
      getWidthFrom: 'body',
      responsiveWidth: true
    });
  }

  /*----------------------------------------------------*/
  // Superslides
  /*----------------------------------------------------*/
  var height = $(window).height() - 150; // 55px + 95px its height of top block.
  //$('#home').height(height);

  $('#slides').superslides({
    play: !isNoviBuilder ? 7000 : false,
    animation: 'fade', // slide
    pagination: false,
    inherit_height_from: '#home'
  });

  // Select2.
  $('.select2').select2({
    // containerCss: ".eeeeeee",
    minimumResultsForSearch: Infinity
  });

  /**
   * @desc Initialize owl carousel plugin
   * @param {object} c - carousel jQuery object
   */
  function initOwlCarousel(c) {
    var aliaces = ["-", "-sm-", "-md-", "-lg-", "-xl-", "-xxl-"],
      values = [0, 576, 768, 992, 1200, 1600],
      responsive = {};

    for (var j = 0; j < values.length; j++) {
      responsive[values[j]] = {};
      for (var k = j; k >= -1; k--) {
        if (!responsive[values[j]]["items"] && c.attr("data" + aliaces[k] + "items")) {
          responsive[values[j]]["items"] = k < 0 ? 1 : parseInt(c.attr("data" + aliaces[k] + "items"), 10);
        }
        if (!responsive[values[j]]["stagePadding"] && responsive[values[j]]["stagePadding"] !== 0 && c.attr("data" + aliaces[k] + "stage-padding")) {
          responsive[values[j]]["stagePadding"] = k < 0 ? 0 : parseInt(c.attr("data" + aliaces[k] + "stage-padding"), 10);
        }
        if (!responsive[values[j]]["margin"] && responsive[values[j]]["margin"] !== 0 && c.attr("data" + aliaces[k] + "margin")) {
          responsive[values[j]]["margin"] = k < 0 ? 30 : parseInt(c.attr("data" + aliaces[k] + "margin"), 10);
        }
      }
    }

    // Enable custom pagination
    if (c.attr('data-dots-custom')) {
      c.on("initialized.owl.carousel", function (event) {
        var carousel = $(event.currentTarget),
          customPag = $(carousel.attr("data-dots-custom")),
          active = 0;

        if (carousel.attr('data-active')) {
          active = parseInt(carousel.attr('data-active'), 10);
        }

        carousel.trigger('to.owl.carousel', [active, 300, true]);
        customPag.find("[data-owl-item='" + active + "']").addClass("active");

        customPag.find("[data-owl-item]").on('click', function (e) {
          e.preventDefault();
          carousel.trigger('to.owl.carousel', [parseInt(this.getAttribute("data-owl-item"), 10), 300, true]);
        });

        carousel.on("translate.owl.carousel", function (event) {
          customPag.find(".active").removeClass("active");
          customPag.find("[data-owl-item='" + event.item.index + "']").addClass("active")
        });
      });
    }

    c.on("initialized.owl.carousel", function () {
      initLightGalleryItem(c.find('[data-lightgallery="item"]'), 'lightGallery-in-carousel');
    });

    c.owlCarousel({
      autoplay: isNoviBuilder ? false : c.attr("data-autoplay") === "true",
      loop: isNoviBuilder ? false : c.attr("data-loop") !== "false",
      items: 1,
      center: c.attr("data-center") === "true",
      dotsContainer: c.attr("data-pagination-class") || false,
      navContainer: c.attr("data-navigation-class") || false,
      mouseDrag: isNoviBuilder ? false : c.attr("data-mouse-drag") !== "false",
      nav: c.attr("data-nav") === "true",
      dots: c.attr("data-dots") === "true",
      dotsEach: c.attr("data-dots-each") ? parseInt(c.attr("data-dots-each"), 10) : false,
      animateIn: c.attr('data-animation-in') ? c.attr('data-animation-in') : false,
      animateOut: c.attr('data-animation-out') ? c.attr('data-animation-out') : false,
      responsive: responsive,
      navText: function () {
        try {
          return JSON.parse(c.attr("data-nav-text"));
        } catch (e) {
          return [];
        }
      }(),
      navClass: function () {
        try {
          return JSON.parse(c.attr("data-nav-class"));
        } catch (e) {
          return ['owl-prev', 'owl-next'];
        }
      }()
    });
  }

  // Owl carousel
  if (plugins.owl.length) {
    for (var i = 0; i < plugins.owl.length; i++) {
      var c = $(plugins.owl[i]);
      plugins.owl[i].owl = c;

      initOwlCarousel(c);
    }
  }

  // Slider range.
  $(".slider-range").slider({
    range: true,
    min: 0,
    max: 120000,
    values: [4500, 100200],
    slide: function (event, ui) {
      $(".amount").val("$" + ui.values[0]);
      $(".amount2").val("$" + ui.values[1]);
    }
  });
  $(".amount").val("$" + $(".slider-range").slider("values", 0));
  $(".amount2").val("$" + $(".slider-range").slider("values", 1));

  // Tabs 2.
  // $('.tabgroup > div').hide();
  // $('.tabgroup > div:first-of-type').show();
  $('.tabs a').click(function (e) {
    e.preventDefault();
    var $this = $(this),
      tabgroup = '#' + $this.parents('.tabs').data('tabgroup'),
      others = $this.closest('li').siblings(),
      target = $this.attr('href');

    others.removeClass('active');
    $this.closest('li').addClass('active');
    $(tabgroup).children('div').hide();
    $(target).show();

    // Fix for G MAP.
    // var map = document.querySelector( e.target.getAttribute( 'href' ) ).querySelector( '.google-map-container' );
    // if ( map ) initGMap([ map ]);
  });
  $('.tabs .active a').click();



  /**
   * @desc Calculate the height of swiper slider basing on data attr
   * @param {object} object - slider jQuery object
   * @param {string} attr - attribute name
   * @return {number} slider height
   */
  function getSwiperHeight(object, attr) {
    var val = object.attr("data-" + attr),
      dim;

    if (!val) {
      return undefined;
    }

    dim = val.match(/(px)|(%)|(vh)|(vw)$/i);

    if (dim.length) {
      switch (dim[0]) {
        case "px":
          return parseFloat(val);
        case "vh":
          return $window.height() * (parseFloat(val) / 100);
        case "vw":
          return $window.width() * (parseFloat(val) / 100);
        case "%":
          return object.width() * (parseFloat(val) / 100);
      }
    } else {
      return undefined;
    }
  }

  /**
   * @desc Initialize the gallery with set of images
   * @param {object} itemsToInit - jQuery object
   * @param {string} addClass - additional gallery class
   */
  function initLightGallery(itemsToInit, addClass) {
    if (!isNoviBuilder) {
      $(itemsToInit).lightGallery({
        thumbnail: $(itemsToInit).attr("data-lg-thumbnail") !== "false",
        selector: "[data-lightgallery='item']",
        autoplay: $(itemsToInit).attr("data-lg-autoplay") === "true",
        pause: parseInt($(itemsToInit).attr("data-lg-autoplay-delay")) || 5000,
        addClass: addClass,
        mode: $(itemsToInit).attr("data-lg-animation") || "lg-slide",
        loop: $(itemsToInit).attr("data-lg-loop") !== "false"
      });
    }
  }

  /**
   * @desc Initialize the gallery with dynamic addition of images
   * @param {object} itemsToInit - jQuery object
   * @param {string} addClass - additional gallery class
   */
  function initDynamicLightGallery(itemsToInit, addClass) {
    if (!isNoviBuilder) {
      $(itemsToInit).on("click", function () {
        $(itemsToInit).lightGallery({
          thumbnail: $(itemsToInit).attr("data-lg-thumbnail") !== "false",
          selector: "[data-lightgallery='item']",
          autoplay: $(itemsToInit).attr("data-lg-autoplay") === "true",
          pause: parseInt($(itemsToInit).attr("data-lg-autoplay-delay")) || 5000,
          addClass: addClass,
          mode: $(itemsToInit).attr("data-lg-animation") || "lg-slide",
          loop: $(itemsToInit).attr("data-lg-loop") !== "false",
          dynamic: true,
          dynamicEl: JSON.parse($(itemsToInit).attr("data-lg-dynamic-elements")) || []
        });
      });
    }
  }

  /**
   * @desc Initialize the gallery with one image
   * @param {object} itemToInit - jQuery object
   * @param {string} addClass - additional gallery class
   */
  function initLightGalleryItem(itemToInit, addClass) {
    if (!isNoviBuilder) {
      $(itemToInit).lightGallery({
        selector: "this",
        addClass: addClass,
        counter: false,
        youtubePlayerParams: {
          modestbranding: 1,
          showinfo: 0,
          rel: 0,
          controls: 0
        },
        vimeoPlayerParams: {
          byline: 0,
          portrait: 0
        }
      });
    }
  }

  // lightGallery
  if (plugins.lightGallery.length) {
    for (var i = 0; i < plugins.lightGallery.length; i++) {
      initLightGallery(plugins.lightGallery[i]);
    }
  }

  // lightGallery item
  if (plugins.lightGalleryItem.length) {
    // Filter carousel items
    var notCarouselItems = [];

    for (var z = 0; z < plugins.lightGalleryItem.length; z++) {
      if (!$(plugins.lightGalleryItem[z]).parents('.owl-carousel').length &&
        !$(plugins.lightGalleryItem[z]).parents('.swiper-slider').length &&
        !$(plugins.lightGalleryItem[z]).parents('.slick-slider').length) {
        notCarouselItems.push(plugins.lightGalleryItem[z]);
      }
    }

    plugins.lightGalleryItem = notCarouselItems;

    for (var i = 0; i < plugins.lightGalleryItem.length; i++) {
      initLightGalleryItem(plugins.lightGalleryItem[i]);
    }
  }

  // Dynamic lightGallery
  if (plugins.lightDynamicGalleryItem.length) {
    for (var i = 0; i < plugins.lightDynamicGalleryItem.length; i++) {
      initDynamicLightGallery(plugins.lightDynamicGalleryItem[i]);
    }
  }

  // Swiper
  if (plugins.swiper.length) {
    for (var i = 0; i < plugins.swiper.length; i++) {
      var s = $(plugins.swiper[i]);
      var pag = s.find(".swiper-pagination"),
        next = s.find(".swiper-button-next"),
        prev = s.find(".swiper-button-prev"),
        bar = s.find(".swiper-scrollbar"),
        swiperSlide = s.find(".swiper-slide"),
        autoplay = false;

      for (var j = 0; j < swiperSlide.length; j++) {
        var $this = $(swiperSlide[j]),
          url;

        if (url = $this.attr("data-slide-bg")) {
          $this.css({
            "background-image": "url(" + url + ")",
            "background-size": "cover"
          })
        }
      }

      swiperSlide.end()
        .find("[data-caption-animate]")
        .addClass("not-animated")
        .end();

      s.swiper({
        autoplay: s.attr('data-autoplay') ? s.attr('data-autoplay') === "false" ? undefined : s.attr('data-autoplay') : 5000,
        direction: s.attr('data-direction') ? s.attr('data-direction') : "horizontal",
        effect: s.attr('data-slide-effect') ? s.attr('data-slide-effect') : "slide",
        speed: s.attr('data-slide-speed') ? s.attr('data-slide-speed') : 600,
        keyboardControl: s.attr('data-keyboard') === "true",
        mousewheelControl: s.attr('data-mousewheel') === "true",
        mousewheelReleaseOnEdges: s.attr('data-mousewheel-release') === "true",
        nextButton: next.length ? next.get(0) : null,
        prevButton: prev.length ? prev.get(0) : null,
        pagination: pag.length ? pag.get(0) : null,
        paginationClickable: pag.length ? pag.attr("data-clickable") !== "false" : false,
        paginationBulletRender: pag.length ? pag.attr("data-index-bullet") === "true" ? function (swiper, index, className) {
          return '<span class="' + className + '">' + (index + 1) + '</span>';
        } : null : null,
        scrollbar: bar.length ? bar.get(0) : null,
        scrollbarDraggable: bar.length ? bar.attr("data-draggable") !== "false" : true,
        scrollbarHide: bar.length ? bar.attr("data-draggable") === "false" : false,
        loop: isNoviBuilder ? false : s.attr('data-loop') !== "false",
        simulateTouch: s.attr('data-simulate-touch') && !isNoviBuilder ? s.attr('data-simulate-touch') === "true" : false,


      });

      $window.on("resize", (function (s) {
        return function () {
          var mh = getSwiperHeight(s, "min-height"),
            h = getSwiperHeight(s, "height");
          if (h) {
            s.css("height", mh ? mh > h ? mh : h : h);
          }
        }
      })(s)).trigger("resize");
    }
  }

  /*----------------------------------------------------*/
  // To top
  /*----------------------------------------------------*/
  if (!isNoviBuilder) {

    $().UItoTop({ easingType: 'easeOutQuart' });
  }

  /*----------------------------------------------------*/
  // Appear
  /*----------------------------------------------------*/
  $('.animated').appear(function () {
    var elem = $(this);
    var animation = elem.data('animation');
    if (!elem.hasClass('visible')) {
      var animationDelay = elem.data('animation-delay');
      if (animationDelay) {
        setTimeout(function () {
          elem.addClass(animation + " visible");
        }, animationDelay);
      } else {
        elem.addClass(animation + " visible");
      }
    }
  });

  // Copyright Year (Evaluates correct copyright year)
  if (plugins.copyrightYear.length) {
    plugins.copyrightYear.text(initialDate.getFullYear());
  }

  // Animate number
  $('.animated-number').appear(function () {
    var elem = $(this);
    var b = elem.text();
    var d = elem.data('duration');
    var animationDelay = elem.data('animation-delay');
    if (!animationDelay) {
      animationDelay = 0;
    }
    elem.text("0");

    setTimeout(function () {
      elem.animate({num: b}, {
        duration: d,
        step: function (num) {
          this.innerHTML = (num).toFixed(0)
        }
      });
    }, animationDelay);
  });


});

/////////////////////// load
$(window).load(function () {

  /*----------------------------------------------------*/
  // flexslider
  /*----------------------------------------------------*/

    var o = $('#carousel');
    if (o.length > 0) {
        o.flexslider({
            animation: "slide",
            controlNav: false,
            animationLoop: false,
            slideshow: false,
            itemWidth: 121,
            itemMargin: 13,
            asNavFor: '#gslider'
        });
    };

});