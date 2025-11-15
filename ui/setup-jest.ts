import { setupZoneTestEnv } from 'jest-preset-angular/setup-env/zone';

setupZoneTestEnv();

// Global console.error suppression for all tests
// This prevents expected error logs from cluttering test output
// We keep this active throughout the entire test run to catch async errors
const originalConsoleError = console.error;
console.error = jest.fn((...args) => {
  // Silently ignore console.error calls during tests
  // Uncomment the line below to see errors during development
  // originalConsoleError(...args);
});
