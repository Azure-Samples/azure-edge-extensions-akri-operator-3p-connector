// server.js
const basicAuth = require('basic-auth');
const express = require("express");
const app = express();

const port = 80;

const USERNAME = process.env.SERVICE_USERNAME;
const PASSWORD = process.env.SERVICE_PASSWORD;

// In-memory store for users (for demonstration purposes)
const users = {
    [USERNAME]: PASSWORD
};

// Middleware to check Basic Authentication
const authenticate = (req, res, next) => {
    const user = basicAuth(req);
    if (user && users[user.name] === user.pass) {
        next();
    } else {
        res.set('WWW-Authenticate', 'Basic realm="example"');
        res.status(401).json({ error: 'Unauthorized' });
    }
};

// Apply the authentication middleware to all routes
app.use(authenticate);

app.get("/api/machine/my_thermostat_1/status", (req, res) => {
    const contextList = {
        desired_temperature: "91",
        actual_temperature: "85"
    };
    
    res.json(contextList);
});

app.get("/api/machine/my_thermostat_2/status_readonly", (req, res) => {
    const contextList = {
        actual_temperature: "82"
    };
    
    res.json(contextList);
});

app.listen(80, () => {
    console.log(`Server running on port ${port}`);
});