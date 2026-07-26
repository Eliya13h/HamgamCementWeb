import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = "hamgamcementweb.client";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

if (!fs.existsSync(baseFolder)) {
    fs.mkdirSync(baseFolder, { recursive: true });
}

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    if (0 !== child_process.spawnSync('dotnet', [
        'dev-certs',
        'https',
        '--export-path',
        certFilePath,
        '--format',
        'Pem',
        '--no-password',
    ], { stdio: 'inherit', }).status) {
        throw new Error("Could not create certificate.");
    }
}

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
    env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7294';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    build: {
        rollupOptions: {
            output: {
                entryFileNames: 'assets/[name]-[hash].js',
                chunkFileNames: 'assets/pages/[name]-[hash].js',
                assetFileNames: 'assets/[name]-[hash][extname]',
                manualChunks(id) {
                    if (id.includes('node_modules')) {
                        if (id.includes('react-router')) return 'vendor-router'
                        if (id.includes('react-dom') || id.includes('react/')) return 'vendor-react'
                        if (id.includes('bootstrap')) return 'vendor-bootstrap'
                        if (id.includes('datatables.net')) return 'vendor-datatables'
                        return 'vendor'
                    }
                },
            },
        },
    },
    optimizeDeps: {
        include: ['react-multi-date-picker', 'react-date-object'],
    },
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        proxy: {
            '^/api': {
                target,
                secure: false
            },
            '^/uploads': {
                target,
                secure: false,
            },
            '^/report-viewer': {
                target,
                secure: false,
            },
        },
        port: parseInt(env.DEV_SERVER_PORT || '61829'),
        https: {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        }
    }
})
