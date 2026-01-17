# FloraReview Application

This application allows users to open an Sqlite3 database, select entries, and display original and AI-modified text for editing and approval. Approved entries can then be exported to a CSV file using `\t` as a delimiter.

## Database Schema

The application assumes the database schema as defined in `schemas.sql`. It updates the `descriptions` table, specifically the `ApprovedText`, `Status`, `Reviewer`, and `Comments` columns. The original and AI-modified text columns are not altered.

## License and Disclaimer

This software is freeware and is provided "as is" without any warranty. The author is not responsible for any damages or losses caused by the use of this software. By using this software, you agree to these terms.

The source code for this software is freely available on GitHub: https://github.com/stoflom/FloraReview
