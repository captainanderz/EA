FROM node:18-alpine AS build 
WORKDIR /app 
COPY ./ . 
RUN npm install
RUN npm run build

FROM nginx:1-alpine
# Copy the build output to replace the default nginx contents.
COPY --from=build /app/dist/shopping-app /usr/share/nginx/html
COPY /nginx.conf  /etc/nginx/conf.d/default.conf
# Expose port 80
EXPOSE 80