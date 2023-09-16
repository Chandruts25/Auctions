/**
 * @module       Custom Waypoints
 * @author       Evgeniy Gusarov
 * @see          https://ua.linkedin.com/pub/evgeniy-gusarov/8a/a40/54a
 * @license      MIT License
 */
!function(t){var o=t("[data-waypoint-to]");o.length&&t(document).ready(function(){o.each(function(){var o=t(this);o.on("click",function(n){n.preventDefault(),t("body, html").stop().animate({scrollTop:t(o.attr("data-waypoint-to")).offset().top},800)})})})}(jQuery);
