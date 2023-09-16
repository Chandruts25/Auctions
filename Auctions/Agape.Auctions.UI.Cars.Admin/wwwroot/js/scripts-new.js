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

		$window = $(window),
		isIE = userAgent.indexOf("msie") !== -1 ? parseInt(userAgent.split("msie")[1], 10) : userAgent.indexOf("trident") !== -1 ? 11 : userAgent.indexOf("edge") !== -1 ? 12 : false,
		isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent),

		plugins = {
			pointerEvents: isIE < 11 ? "js/pointer-events.min.js" : false,
			bootstrapTabs: $(".tabs-custom-init"),
			materialParallax: $(".parallax-container"),
			maps: $(".google-map-container"),
			superfish: $('.sf-menu'),
			rdMailForm: $(".rd-mailform"),
			rdInputLabel: $(".form-label"),
			regula: $("[data-constraints]"),
			radio: $("input[type='radio']"),
			checkbox: $("input[type='checkbox']"),
			captcha: $('.recaptcha')
		};

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
		var i;

		if (!isNoviBuilder && !isIE && !isMobile) {
			plugins.materialParallax.parallax();
		} else {
			for (i = 0; i < plugins.materialParallax.length; i++) {
				var parallax = $(plugins.materialParallax[i]),
					imgPath = parallax.data("parallax-img");

				parallax.css({
					"background-image": 'url(' + imgPath + ')',
					"background-attachment": "fixed",
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

	/*----------------------------------------------------*/
	// Google map
	/*----------------------------------------------------*/
	// Google map function for getting latitude and longitude
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

	function initGMap ( mapSelector ) {
		$.getScript("//maps.google.com/maps/api/js?key=AIzaSyAwH60q5rWrS8bXwpkZwZwhw9Bw0pqKTZM&sensor=false&libraries=geometry,places&v=3.7", function () {
			var head = document.getElementsByTagName('head')[0],
				insertBefore = head.insertBefore;

			head.insertBefore = function (newElement, referenceElement) {
				if (newElement.href && newElement.href.indexOf('//fonts.googleapis.com/css?family=Roboto') !== -1 || newElement.innerHTML.indexOf('gm-style') !== -1) {
					return;
				}
				insertBefore.call(head, newElement, referenceElement);
			};
			var geocoder = new google.maps.Geocoder;
			for (var i = 0; i < mapSelector.length; i++) {
				var zoom = parseInt(mapSelector[i].getAttribute("data-zoom"), 10) || 11;
				var styles = mapSelector[i].hasAttribute('data-styles') ? JSON.parse(mapSelector[i].getAttribute("data-styles")) : [];
				var center = mapSelector[i].getAttribute("data-center") || "New York";

				// Initialize map
				var map = new google.maps.Map(mapSelector[i].querySelectorAll(".google-map")[0], {
					zoom: zoom,
					styles: styles,
					scrollwheel: false,
					center: {lat: 0, lng: 0}
				});
				// Add map object to map node
				mapSelector[i].map = map;
				mapSelector[i].geocoder = geocoder;
				mapSelector[i].google = google;

				// Get Center coordinates from attribute
				getLatLngObject(center, null, mapSelector[i], function (location, markerElement, mapElement) {
					mapElement.map.setCenter(location);
				});

				// Add markers from google-map-markers array
				var markerItems = mapSelector[i].querySelectorAll(".google-map-markers li");

				if (markerItems.length) {
					var markers = [];
					for (var j = 0; j < markerItems.length; j++) {
						var markerElement = markerItems[j];
						getLatLngObject(markerElement.getAttribute("data-location"), markerElement, mapSelector[i], function (location, markerElement, mapElement) {
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
							if (icon) {
								markerData.icon = icon;
							}
							var marker = new google.maps.Marker(markerData);
							markerElement.gmarker = marker;
							markers.push({markerElement: markerElement, infoWindow: infoWindow});
							marker.isActive = false;
							// Handle infoWindow close click
							google.maps.event.addListener(infoWindow, 'closeclick', (function (markerElement, mapElement) {
								var markerIcon = null;
								markerElement.gmarker.isActive = false;
								markerIcon = markerElement.getAttribute("data-icon") || mapElement.getAttribute("data-icon");
								markerElement.gmarker.setIcon(markerIcon);
							}).bind(this, markerElement, mapElement));

							// Set marker active on Click and open infoWindow
							google.maps.event.addListener(marker, 'click', (function (markerElement, mapElement) {
								if (markerElement.infoWindow.getContent().length === 0) return;
								var gMarker, currentMarker = markerElement.gmarker, currentInfoWindow;
								for (var k = 0; k < markers.length; k++) {
									var markerIcon;
									if (markers[k].markerElement === markerElement) {
										currentInfoWindow = markers[k].infoWindow;
									}
									gMarker = markers[k].markerElement.gmarker;
									if (gMarker.isActive && markers[k].markerElement !== markerElement) {
										gMarker.isActive = false;
										markerIcon = markers[k].markerElement.getAttribute("data-icon") || mapElement.getAttribute("data-icon")
										gMarker.setIcon(markerIcon);
										markers[k].infoWindow.close();
									}
								}

								currentMarker.isActive = !currentMarker.isActive;
								if (currentMarker.isActive) {
									if (markerIcon = markerElement.getAttribute("data-icon-active") || mapElement.getAttribute("data-icon-active")) {
										currentMarker.setIcon(markerIcon);
									}

									currentInfoWindow.open(map, marker);
								} else {
									if (markerIcon = markerElement.getAttribute("data-icon") || mapElement.getAttribute("data-icon")) {
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
	if ( plugins.maps.length ) {
		var tmp = [];

		for ( var i = 0; i < plugins.maps.length; i++ ) {
			var map = $( plugins.maps[i] );
			if ( map.parents( '.tabgroup' ).length === 0 ) {
				tmp.push( plugins.maps[i] );
			}
		}

		initGMap( tmp );
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

	/*----------------------------------------------------*/
	// Superfish menu
	/*----------------------------------------------------*/
	if (plugins.superfish.length) {
		if (!isNoviBuilder) {
			plugins.superfish.superfish();
		} else {
			var sfMenu = $('.sf-menu > li'),
				dropdownMenu = sfMenu.has('ul');

			dropdownMenu.append("<span class='dropdown-toggle'></span>");

			var toggle = $('.dropdown-toggle');

			toggle.on('click', function () {
				$('.sf-menu > li ul').toggle();
			});
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

	// Slider range.
	$("#slider-range").slider({
		range: true,
		min: 0,
		max: 120000,
		values: [4500, 100200],
		slide: function (event, ui) {
			$("#amount").val("$" + ui.values[0]);
			$("#amount2").val("$" + ui.values[1]);
		}
	});
	$("#amount").val("$" + $("#slider-range").slider("values", 0));
	$("#amount2").val("$" + $("#slider-range").slider("values", 1));

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
		var map = [ document.querySelector( e.target.getAttribute( 'href' ) ).querySelector( '.google-map-in-tab' ) ];
		if ( map.length ) {
			initGMap( map );
		}
	});
	// $('.tabs .active a').click();


	/*----------------------------------------------------*/
	// MENU SMOOTH SCROLLING
	/*----------------------------------------------------*/
	if (!isNoviBuilder) {
		$(".navbar_ .nav a, .menu_bot a, .scroll-to").bind('click', function (event) {

			//$(".navbar_ .nav a a").removeClass('active');
			//$(this).addClass('active');
			// var headerH = $('#top1').outerHeight();
			var headerH = $('#top2').outerHeight();

			if ($(this).attr("href") == "#home") {
				$("html, body").animate({
					scrollTop: 0 + 'px'
					// scrollTop: $($(this).attr("href")).offset().top + 'px'
				}, {
					duration: 1200,
					easing: "easeInOutExpo"
				});
			}
			else {
				$("html, body").animate({
					scrollTop: $($(this).attr("href")).offset().top - headerH + 'px'
					// scrollTop: $($(this).attr("href")).offset().top + 'px'
				}, {
					duration: 1200,
					easing: "easeInOutExpo"
				});
			}
			event.preventDefault();
		});
	}

	/*----------------------------------------------------*/
	// To top
	/*----------------------------------------------------*/
	if (!isNoviBuilder) {
		/*
		var defaults = {
			containerID: 'toTop', // fading element id
			containerHoverID: 'toTopHover', // fading element hover id
			scrollSpeed: 1200,
			easingType: 'linear'
			};
		*/

		$().UItoTop({ easingType: 'easeOutQuart' });
	}

	/*----------------------------------------------------*/
	// Appear
	/*----------------------------------------------------*/
	$('.animated').appear(function () {
		// console.log("111111111111");
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


	/////// flexslider
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
	}
	;


	var o = $('#gslider');
	if (o.length > 0) {
		o.flexslider({
			animation: "slide",
			controlNav: false,
			animationLoop: false,
			slideshow: false,
			sync: "#carousel",
			start: function (slider) {
				// $('body').removeClass('loading');
			}
		});
	}


});