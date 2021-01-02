#!/bin/bash

if [ ! -f gerber_files ]; then
	mkdir gerber_files
fi

pushd gerber_files
rm *
unzip ../monitorBuddy_Gerber.Zip
unzip -o ../monitorBuddy_NC_Drill.Zip	

rm "Status Report.Txt"
rm ../monitorBuddy_all.zip
zip ../monitorBuddy_all.zip *
