# master-thesis-codeql-perf-test

Script for extracting the Source Level Model (SLM) from C# source code. The results include processes and dataflow candidates in a CSV file.

- CodeQL module *callpoints.qll* contains the code for program Entry- and Exit-Point analysis.
- CodeQL script *identify-call-paths.ql* contains the code for call path analysis.
- Bash script *run-analysis.sh* contains instructions for a full iteration of CodeQL db create, query run and convert results + timing of these operations.
