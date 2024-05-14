using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Task_Maze
{
    public partial class FormMaze : Form
    {
        private const int CellSize = 30;
        private const int WallChance = 20;  // Шанс, с которым клетка станет стеной (в процентах)

        private int width;
        private int height;
        private Point startPoint;
        private Point endPoint;
        private bool[,] maze;
        private bool isGenerating = false;
        private List<Point> shortestPath;

        public FormMaze()
        {
            InitializeComponent();
        }


        private bool[,] GenerateMaze(int width, int height)
        {
            bool[,] maze = new bool[width, height];

            Random rand = new Random();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    maze[x, y] = rand.Next(100) < WallChance;
                }
            }

            // Установка начальной и конечной точки
            startPoint = new Point(rand.Next(width), rand.Next(height));
            endPoint = new Point(rand.Next(width), rand.Next(height));

            maze[startPoint.X, startPoint.Y] = false;  // Начальная точка не является стеной
            maze[endPoint.X, endPoint.Y] = false;      // Конечная точка не является стеной

            return maze;
        }

        private bool FindPath(Point current, Point end, bool[,] visited)
        {
            if (current == end)
                return true;  // Путь найден

            // Массив смещений для соседних клеток: вверх, вниз, влево, вправо
            Point[] directions = { new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0) };

            foreach (var dir in directions)
            {
                Point next = new Point(current.X + dir.X, current.Y + dir.Y);

                // Проверка, что следующая клетка находится в пределах лабиринта и не является стеной
                if (next.X >= 0 && next.X < width && next.Y >= 0 && next.Y < height && !maze[next.X, next.Y] && !visited[next.X, next.Y])
                {
                    visited[next.X, next.Y] = true;  // Помечаем клетку как посещенную

                    if (FindPath(next, end, visited))
                    {
                        // Путь найден, помечаем клетку как часть пути
                        maze[next.X, next.Y] = true;
                        return true;
                    }
                }
            }

            return false;  // Путь не найден
        }

        private void FindPath(Point start, Point end)
        {
            bool[,] visited = new bool[width, height];
            visited[start.X, start.Y] = true;

            if (!FindPath(start, end, visited))
            {
                MessageBox.Show("Путь не найден!");
            }
        }

        private List<Point> FindShortestPath(Point start, Point end)
        {
            Queue<Point> queue = new Queue<Point>();
            Dictionary<Point, Point> cameFrom = new Dictionary<Point, Point>();
            bool[,] visited = new bool[width, height];

            queue.Enqueue(start);
            visited[start.X, start.Y] = true;

            while (queue.Count > 0)
            {
                Point current = queue.Dequeue();

                if (current == end)
                {
                    // Построение пути от конечной точки к начальной
                    List<Point> path = new List<Point>();
                    Point currentPoint = end;

                    while (currentPoint != start)
                    {
                        path.Add(currentPoint);
                        currentPoint = cameFrom[currentPoint];
                    }

                    path.Add(start);
                    path.Reverse();
                    return path;
                }

                Point[] directions = { new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0) };

                foreach (var dir in directions)
                {
                    Point next = new Point(current.X + dir.X, current.Y + dir.Y);

                    if (next.X >= 0 && next.X < width && next.Y >= 0 && next.Y < height && !maze[next.X, next.Y] && !visited[next.X, next.Y])
                    {
                        queue.Enqueue(next);
                        visited[next.X, next.Y] = true;
                        cameFrom[next] = current;
                    }
                }
            }

            return null; // Путь не найден
        }


        private void buttonGenerate_Click(object sender, EventArgs e)
        {
            if (isGenerating)
                return;

            if (!int.TryParse(textBoxWidth.Text, out width) || !int.TryParse(textBoxHeight.Text, out height))
            {
                MessageBox.Show("Введите действительные значения ширины и высоты.");
                return;
            }
            

            startPoint = Point.Empty;
            endPoint = Point.Empty;

            maze = GenerateMaze(width, height);
            pictureBoxMaze.Invalidate();

            ThreadPool.QueueUserWorkItem(state => FindPath(startPoint, endPoint));
        }

        private void pictureBoxMaze_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            

            if (maze == null)
                return;

            

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Brush brush = maze[x, y] ? Brushes.Black : Brushes.White;
                    g.FillRectangle(brush, x * CellSize, y * CellSize, CellSize, CellSize);
                    g.DrawRectangle(Pens.Gray, x * CellSize, y * CellSize, CellSize, CellSize);
                }
            }

            // Отображение начальной и конечной точки
            g.FillEllipse(Brushes.Green, startPoint.X * CellSize, startPoint.Y * CellSize, CellSize, CellSize);
            g.FillEllipse(Brushes.Red, endPoint.X * CellSize, endPoint.Y * CellSize, CellSize, CellSize);

            if (shortestPath != null)
            {

                Pen pen = new Pen(Color.Red, 5);

                for (int i = 0; i < shortestPath.Count - 1; i++)
                {
                    Point p1 = shortestPath[i];
                    Point p2 = shortestPath[i + 1];
                    g.DrawLine(pen, p1.X * CellSize + CellSize / 2, p1.Y * CellSize + CellSize / 2, p2.X * CellSize + CellSize / 2, p2.Y * CellSize + CellSize / 2);
                }
                pen.Dispose();
            }
        }

        private void pictureBoxMaze_MouseClick(object sender, MouseEventArgs e)
        {
            int x = e.X / CellSize;
            int y = e.Y / CellSize;

            if (x < 0 || x >= width || y < 0 || y >= height)
                return;

            if (e.Button == MouseButtons.Left)
            {
                startPoint = new Point(x, y);
            }
            else if (e.Button == MouseButtons.Right)
            {
                endPoint = new Point(x, y);
            }

            maze[startPoint.X, startPoint.Y] = false;  // Начальная точка не является стеной
            maze[endPoint.X, endPoint.Y] = false;      // Конечная точка не является стеной

            pictureBoxMaze.Invalidate();
        }

        private async void pictureBoxMaze_MouseMove(object sender, MouseEventArgs e)
        {
            int x = e.X / CellSize;
            int y = e.Y / CellSize;

            if (x < 0 || x >= width || y < 0 || y >= height)
                return;

            Point currentPoint = new Point(x, y);

            if (isGenerating || maze[x, y])
                return;

            shortestPath = await Task.Run(() => FindShortestPath(startPoint, currentPoint));
            //List<Point> shortestPath = FindShortestPath(startPoint, currentPoint);

            if (shortestPath != null)
            {
                // Отобразить кратчайший путь на лабиринте
                // Очистить предыдущий кратчайший путь (если есть)
                pictureBoxMaze.Invalidate();

                //Graphics g = pictureBoxMaze.CreateGraphics();
                //Pen pen = new Pen(Color.Red, 5);

                //for (int i = 0; i < shortestPath.Count - 1; i++)
                //{
                //    Point p1 = shortestPath[i];
                //    Point p2 = shortestPath[i + 1];
                //    g.DrawLine(pen, p1.X * CellSize + CellSize / 2, p1.Y * CellSize + CellSize / 2, p2.X * CellSize + CellSize / 2, p2.Y * CellSize + CellSize / 2);
                //}

                //pen.Dispose();
                //g.Dispose();
            }
            else
            {
                MessageBox.Show("Пути не существует!");
            }
        }
    }
}
