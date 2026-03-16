# Stage 1: Build (Môi trường chứa Node.js để biên dịch code React/Vite)
FROM node:20-alpine AS build
WORKDIR /app

# Copy package.json và package-lock.json để cài dependencies
COPY package*.json ./
RUN npm install

# Copy toàn bộ source code
COPY . .

# Build dự án (chạy Vite build) ra thư mục /app/dist
RUN npm run build

# Stage 2: Serve (Sử dụng Nginx siêu nhẹ để phục vụ file tĩnh cho trình duyệt web)
FROM nginx:alpine AS final
# Copy code đã build sang thư mục host web của Nginx
COPY --from=build /app/dist /usr/share/nginx/html

# Copy file cấu hình tuỳ chỉnh nếu cần (hiện tại Nginx default chạy Port 80, rất tốt)
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]