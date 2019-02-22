var runTime = parseInt(process.argv[2]);

console.log('Working');
setTimeout(() => {
    console.log("Finished");
}, runTime);
