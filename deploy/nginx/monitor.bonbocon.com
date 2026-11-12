# Monitoring behind basic auth: Netdata at /system/, Dozzle at /logs/ (both still bound to 127.0.0.1 only).
# Needs /etc/nginx/conf.d/monitor-ratelimit.conf (the limit_req zone) and /etc/nginx/.htpasswd-monitor.
# Copy to /etc/nginx/sites-available/monitor.bonbocon.com, then:
#   sudo ln -s /etc/nginx/sites-available/monitor.bonbocon.com /etc/nginx/sites-enabled/
#   sudo nginx -t && sudo systemctl reload nginx
#   sudo certbot --nginx -d monitor.bonbocon.com      (while the DNS record is still DNS only)

server {
    # Answer only requests that come through Cloudflare (chef-lan deploy/nginx/snippets/cloudflare-only.conf).
    include snippets/cloudflare-only.conf;
    listen 80;
    listen [::]:80;
    server_name monitor.bonbocon.com;

    auth_basic "Bon Bocon monitoring";
    auth_basic_user_file /etc/nginx/.htpasswd-monitor;

    # Per visitor IP (real IP restored by cloudflare-realip.conf). Generous enough for the dashboards' own polling;
    # the real defence against guessing is a long random password.
    limit_req zone=monitor burst=200 nodelay;
    limit_req_status 429;

    add_header X-Robots-Tag "noindex, nofollow" always;

    location = / {
        return 302 /system/;
    }

    # Netdata has no base-path setting: strip /system/ and pass the rest.
    location = /system {
        return 301 /system/;
    }

    location ~ ^/system/(?<ndpath>.*)$ {
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header Connection "";
        proxy_pass http://127.0.0.1:19999/$ndpath$is_args$args;
    }

    # Dozzle runs with DOZZLE_BASE=/logs, so the path is passed unchanged. Logs stream over server-sent events:
    # no buffering, long reads.
    location = /logs {
        return 301 /logs/;
    }

    location /logs/ {
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header Connection "";
        proxy_buffering off;
        proxy_cache off;
        proxy_read_timeout 1h;
        proxy_pass http://127.0.0.1:8081;
    }
}
