// MongoDB script to drop the old text index on loops collection
// Run this with: mongosh <your-connection-string> drop-old-loop-index.js

// Connect to your database (adjust the database name if needed)
db = db.getSiblingDB('lendingloop');

print('Dropping old text index on loops collection...');

try {
    // Drop the old name_text index
    db.loops.dropIndex('name_text');
    print('✓ Successfully dropped name_text index');
} catch (e) {
    if (e.codeName === 'IndexNotFound') {
        print('✓ Index name_text does not exist (already dropped or never created)');
    } else {
        print('✗ Error dropping index: ' + e.message);
    }
}

print('\nCurrent indexes on loops collection:');
db.loops.getIndexes().forEach(function(index) {
    print('  - ' + index.name + ': ' + JSON.stringify(index.key));
});

print('\nYou can now restart your application to create the new compound text index.');
