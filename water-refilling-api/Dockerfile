FROM php:8.3-apache

RUN apt-get update \
  && apt-get install -y --no-install-recommends git unzip curl libzip-dev libcurl4-openssl-dev libxml2-dev libonig-dev libsqlite3-dev \
    && docker-php-ext-install pdo_mysql pdo_sqlite mbstring bcmath exif pcntl zip curl dom xml \
    && rm -rf /var/lib/apt/lists/*

COPY --from=composer:2 /usr/bin/composer /usr/bin/composer

RUN mkdir -p /etc/ssl/tidb \
    && curl -fsSL https://letsencrypt.org/certs/isrgrootx1.pem \
       -o /etc/ssl/tidb/isrgrootx1.pem

WORKDIR /var/www/html

COPY . .

RUN composer install --no-dev --prefer-dist --no-interaction --optimize-autoloader \
    && sed -ri -e 's!/var/www/html!/var/www/html/public!g' /etc/apache2/sites-available/000-default.conf \
    && a2enmod rewrite \
    && touch database/database.sqlite \
    && chown -R www-data:www-data storage bootstrap/cache database \
    && chmod -R ug+rwx storage bootstrap/cache database

EXPOSE 10000

CMD ["bash", "-lc", "sed -i \"s/Listen 80/Listen ${PORT:-10000}/\" /etc/apache2/ports.conf && sed -i \"s/:80>/:${PORT:-10000}>/\" /etc/apache2/sites-available/000-default.conf && apache2-foreground"]


