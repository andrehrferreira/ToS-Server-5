import * as dotenv from "dotenv";
import { Config } from "@cmmv/core";

dotenv.config();

Config.assign({
    env: process.env.NODE_ENV,

    server: {
        host: process.env.HOST || '0.0.0.0',
        port: process.env.PORT || 5000,
        cors: {
            enabled: false
        },
    },

    repository: {
        type: 'sqlite',
        database: "./database.sqlite",
        synchronize: true,
        logging: false,
    }
});
