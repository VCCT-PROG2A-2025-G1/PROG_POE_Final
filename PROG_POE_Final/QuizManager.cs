using System.Collections.Generic;
using System.Text;
using PROG_POE_Final.Models;

namespace PROG_POE_Final
{
    //---------------------------------------------------------------------------------------------------------------------------------------------
    // Manages cybersecurity quiz, including questions, answers and scoring
    public class QuizManager
    {
        private List<QuizQuestion> questions;
        private int currentQuestionIndex = 0;
        private int score = 0;
        //---------------------------------------------------------------------------------------------------------------------------------------------
        //Default Constructor
        public QuizManager()
        {
            LoadQuestions();
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        // Loads the quiz questions into the list
        private void LoadQuestions()
        {
            questions = new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    Question = "What should you do if you receive an email asking for your password?",
                    Options = new List<string> { "Reply with your password", "Delete it", "Report it as phishing", "Ignore it" },
                    CorrectAnswer = 3,
                    Explanation = "Reporting phishing helps prevent scams."
                },
                new QuizQuestion
                {
                    Question = "True or False: Using '123456' as a password is safe.",
                    Options = new List<string> { "True", "False" },
                    CorrectAnswer = 2,
                    Explanation = "Simple passwords are insecure."
                },
                new QuizQuestion
                {
                    Question = "If a website URL starts with \"https://\" instead of \"http://\", it means:",
                    Options = new List<string> { "The website is government approved", "The website loads faster", "The connection is encrypted and more secure", "The website cannor be hacked" },
                    CorrectAnswer = 3,
                    Explanation = "The \"s\" stands for secure, meaning the data is encrypted between your browser and the website."
                },
                 new QuizQuestion
                {
                    Question = "What is phishing?",
                    Options = new List<string> { "A virus that deletes data", "A scam to trick people into sharing sensitive information", "A type of firewall", "A tool for secure browsing" },
                    CorrectAnswer = 2,
                    Explanation = "Phishing is a form of social engineering using fake emails or websites to steal credentials or information."
                },
                  new QuizQuestion
                {
                    Question = "Which of the following is a safe online practice?",
                    Options = new List<string> { " Use the same password for all accounts", "Click on all email attachments to check them", "Share personal details on public devices", "Regularly update your software and apps"},
                    CorrectAnswer = 4,
                    Explanation = "Updating removes vulnerabilities that hackers can exploit."
                },
                   new QuizQuestion
                {
                    Question = "What is malware?",
                    Options = new List<string> { "A security tool", "Malicious software designed to harm, exploit, or steal", "A password manager"," A type of firewall"  },
                    CorrectAnswer = 2,
                    Explanation = "Malware refers to viruses, worms, ransomware, spyware, and other harmful software.."
                },
                new QuizQuestion
                {
                    Question = "When using public Wi-Fi, what should you avoid doing?",
                    Options = new List<string> { "Browsing news websites", "Logging into your bank or sensitive accounts without a VPN", "Watching YouTube videos", "Reading articles" },
                    CorrectAnswer = 2,
                    Explanation = "Public Wi-Fi can be insecure. Logging into sensitive accounts can expose your credentials unless using a VPN."
                },

                new QuizQuestion
                {
                    Question = "Which is the safest way to verify a link in an email?",
                    Options = new List<string> { "True Click it quickly to see where it leads", " Forward it to a friend and ask them", "Hover your mouse over the link to check the real URL","Reply to the email asking if it's legitimate" },
                    CorrectAnswer = 3,
                    Explanation = "Hovering reveals the actual link destination without clicking, helping detect phishing."
                },
                new QuizQuestion
                {
                    Question = "Which of the following is the strongest password?",
                    Options = new List<string> { "password123", "John1987", "V&9#qP!7gLz%", "12345678" },
                    CorrectAnswer = 3,
                    Explanation = "Complex passwords with a mix of letters, numbers, and symbols are much harder to guess or crack."
                },
                 new QuizQuestion
                {
                    Question = "What should you do if you suspect your device has malware?",
                    Options = new List<string> { "Keep using it and hope it goes away", " Turn off Wi-Fi but ignore the problem", "Run an antivirus scan and remove suspicious files", "Buy a new device immediately" },
                    CorrectAnswer = 3,
                    Explanation = "Running a trusted antivirus or anti-malware scan is the first step to detect and remove threats."
                }
                
            };
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        //Gets the current question the user is on
        public QuizQuestion GetCurrentQuestion()
        {
            if (currentQuestionIndex < questions.Count)
            {
                return questions[currentQuestionIndex];
            }
            else
            {
                return null;
            }
               
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        // Handles the user choice of answer
        public string SubmitAnswer(int answerIndex)
        {
            if (currentQuestionIndex >= questions.Count)
            {
                return "The quiz is over.";
            }
                

            var question = questions[currentQuestionIndex];
            string feedback;

            // Check if the user's input matches the correct answer
            if (answerIndex + 1 == question.CorrectAnswer)
            {
               score++;
                feedback = $"Correct! {question.Explanation}";
            }
            else
            {
                feedback = $"Incorrect. {question.Explanation}";
            }

            currentQuestionIndex++;

            //---------------------------------------------------------------------------------------------------------------------------------------------
            //Show the final score and feedback
            if (currentQuestionIndex == questions.Count)
            {
                feedback += $"\n\nQuiz completed! Your score: {score}/{questions.Count}.";
                if (score >= questions.Count * 0.8)
                    feedback += "\nGreat job! You're a cybersecurity pro!";
                else if (score >= questions.Count * 0.5)
                    feedback += "\nGood effort! Keep learning to stay safe online!";
                else
                    feedback += "\nKeep practicing to improve your cybersecurity knowledge!";
                ResetQuiz();
            }

            return feedback;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        // Starts the quiz over from the first question
        public void RestartQuiz()
        {
            currentQuestionIndex = 0;
            score = 0;
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------
        // Marks the quiz as completed 
        private void ResetQuiz()
        {
            currentQuestionIndex = questions.Count;
        }
    }
}
//----------------------------------------------------------------------------------END OF FILE-----------------------------------------------------------------------