using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GrafoElSalvador
{
    public class Form1 : Form
    {
        // Controles
        private PictureBox picMapa;
        private ComboBox cmbDepartamentos;
        private Button btnAgregarNodo;
        private Button btnConectar;
        private Button btnRecorrerAnchura;
        private Button btnRecorrerProfundidad;
        private ListBox lstResultados;
        private Panel panelControles;
        private Panel panelFondo;

        // Datos del grafo
        private Grafo grafo = new Grafo();
        private Nodo nodoSeleccionado = null;
        private bool modoConectar = false;

        // Coordenadas aproximadas de las cabeceras departamentales
        private Dictionary<string, Point> coordenadasCabeceras = new Dictionary<string, Point>
        {
            { "Ahuachapán", new Point(100, 400) },
            { "Santa Ana", new Point(150, 350) },
            { "Sonsonate", new Point(120, 450) },
            { "Chalatenango", new Point(250, 300) },
            { "La Libertad", new Point(300, 400) },
            { "San Salvador", new Point(350, 380) },
            { "Cuscatlán", new Point(400, 350) },
            { "La Paz", new Point(450, 450) },
            { "Cabañas", new Point(500, 300) },
            { "San Vicente", new Point(550, 350) },
            { "Usulután", new Point(600, 400) },
            { "San Miguel", new Point(650, 300) },
            { "Morazán", new Point(700, 250) },
            { "La Unión", new Point(750, 350) }
        };

        public Form1()
        {
            InitializeComponent();
            InicializarGrafo();
        }

        private void InitializeComponent()
        {
            // Configuración básica del formulario
            this.Text = "Grafo de Departamentos de El Salvador";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // Panel principal con scroll
            panelFondo = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            this.Controls.Add(panelFondo);

            // PictureBox para el mapa (más grande que el área visible)
            picMapa = new PictureBox
            {
                Size = new Size(1500, 1000),
                Location = new Point(0, 0),
                BackColor = Color.White,
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            try
            {
                picMapa.Image = Image.FromFile("mapa_elsalvador_detallado.png");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar el mapa: " + ex.Message);
                // Crear un mapa alternativo si no se encuentra la imagen
                Bitmap bmp = new Bitmap(picMapa.Width, picMapa.Height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.FillRectangle(Brushes.LightBlue, 0, 0, bmp.Width, bmp.Height);
                    g.DrawString("Mapa de El Salvador", new Font("Arial", 20), Brushes.Black, 100, 100);
                }
                picMapa.Image = bmp;
            }

            panelFondo.Controls.Add(picMapa);

            // Panel para los controles (flotante)
            panelControles = new Panel
            {
                Size = new Size(300, 300),
                BackColor = Color.FromArgb(220, Color.LightGray),
                Location = new Point(10, 10),
                BorderStyle = BorderStyle.FixedSingle
            };

            // ComboBox para seleccionar departamentos
            cmbDepartamentos = new ComboBox
            {
                Location = new Point(10, 20),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            string[] departamentos = {
                "Ahuachapán", "Santa Ana", "Sonsonate", "Chalatenango",
                "La Libertad", "San Salvador", "Cuscatlán", "La Paz",
                "Cabañas", "San Vicente", "Usulután", "San Miguel",
                "Morazán", "La Unión"
            };
            cmbDepartamentos.Items.AddRange(departamentos);

            // Botones
            btnAgregarNodo = new Button
            {
                Text = "Agregar Departamento",
                Location = new Point(10, 60),
                Width = 200
            };
            btnAgregarNodo.Click += BtnAgregarNodo_Click;

            btnConectar = new Button
            {
                Text = "Conectar Departamentos",
                Location = new Point(10, 100),
                Width = 200
            };
            btnConectar.Click += BtnConectar_Click;

            btnRecorrerAnchura = new Button
            {
                Text = "Recorrido en Anchura",
                Location = new Point(10, 140),
                Width = 200
            };
            btnRecorrerAnchura.Click += BtnRecorrerAnchura_Click;

            btnRecorrerProfundidad = new Button
            {
                Text = "Recorrido en Profundidad",
                Location = new Point(10, 180),
                Width = 200
            };
            btnRecorrerProfundidad.Click += BtnRecorrerProfundidad_Click;

            // ListBox para resultados
            lstResultados = new ListBox
            {
                Location = new Point(10, 220),
                Width = 200,
                Height = 60
            };

            panelControles.Controls.Add(cmbDepartamentos);
            panelControles.Controls.Add(btnAgregarNodo);
            panelControles.Controls.Add(btnConectar);
            panelControles.Controls.Add(btnRecorrerAnchura);
            panelControles.Controls.Add(btnRecorrerProfundidad);
            panelControles.Controls.Add(lstResultados);

            panelFondo.Controls.Add(panelControles);

            // Evento para dibujar conexiones
            picMapa.Paint += PicMapa_Paint;
            picMapa.MouseClick += PicMapa_MouseClick;
        }

        private void InicializarGrafo()
        {
            // Agregar todos los departamentos con sus cabeceras
            foreach (var depto in coordenadasCabeceras)
            {
                grafo.AgregarNodo(depto.Key, depto.Key, depto.Value);
            }
        }

        private void BtnAgregarNodo_Click(object sender, EventArgs e)
        {
            if (cmbDepartamentos.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un departamento.");
                return;
            }

            string departamento = cmbDepartamentos.SelectedItem.ToString();
            Point ubicacion = coordenadasCabeceras[departamento];

            // Verificar si el nodo ya existe
            if (grafo.Nodos.Exists(n => n.Departamento == departamento))
            {
                MessageBox.Show($"El departamento {departamento} ya está agregado.");
                return;
            }

            grafo.AgregarNodo(departamento, departamento, ubicacion);
            picMapa.Invalidate();
            MessageBox.Show($"Departamento agregado: {departamento}");
        }

        private void BtnConectar_Click(object sender, EventArgs e)
        {
            modoConectar = !modoConectar;
            btnConectar.BackColor = modoConectar ? Color.LightGreen : SystemColors.Control;
            nodoSeleccionado = null;
            MessageBox.Show(modoConectar ?
                "Modo conexión activado. Selecciona el primer departamento." :
                "Modo conexión desactivado.");
        }

        private void PicMapa_MouseClick(object sender, MouseEventArgs e)
        {
            if (!modoConectar) return;

            foreach (Nodo nodo in grafo.Nodos)
            {
                // Verificar si se hizo clic cerca de un nodo (10px de margen)
                if (Math.Abs(e.Location.X - nodo.Ubicacion.X) < 15 &&
                    Math.Abs(e.Location.Y - nodo.Ubicacion.Y) < 15)
                {
                    if (nodoSeleccionado == null)
                    {
                        nodoSeleccionado = nodo;
                        MessageBox.Show($"Seleccionado: {nodo.Departamento}. Ahora elige destino.");
                    }
                    else if (nodoSeleccionado != nodo)
                    {
                        grafo.ConectarNodos(nodoSeleccionado.Departamento, nodo.Departamento);
                        picMapa.Invalidate();
                        MessageBox.Show($"Conectado: {nodoSeleccionado.Departamento} -> {nodo.Departamento}");
                        nodoSeleccionado = null;
                    }
                    else
                    {
                        MessageBox.Show("No puedes conectar un departamento consigo mismo.");
                    }
                    return;
                }
            }
        }

        private void PicMapa_Paint(object sender, PaintEventArgs e)
        {
            // Dibujar nodos
            foreach (Nodo nodo in grafo.Nodos)
            {
                // Dibuja círculo
                e.Graphics.FillEllipse(Brushes.Blue, nodo.Ubicacion.X - 10, nodo.Ubicacion.Y - 10, 20, 20);
                // Dibuja nombre
                e.Graphics.DrawString(nodo.Departamento, new Font("Arial", 8), Brushes.Black,
                    nodo.Ubicacion.X + 12, nodo.Ubicacion.Y - 10);
            }

            // Dibujar conexiones
            Pen lapiz = new Pen(Color.Red, 2);
            foreach (Nodo nodo in grafo.Nodos)
            {
                foreach (Nodo vecino in nodo.Vecinos)
                {
                    e.Graphics.DrawLine(lapiz, nodo.Ubicacion, vecino.Ubicacion);
                }
            }
        }

        private void BtnRecorrerAnchura_Click(object sender, EventArgs e)
        {
            if (cmbDepartamentos.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un departamento de inicio.");
                return;
            }

            string inicio = cmbDepartamentos.SelectedItem.ToString();
            List<string> recorrido = grafo.RecorridoAnchura(inicio);

            lstResultados.Items.Clear();
            lstResultados.Items.Add($"Recorrido en Anchura desde {inicio}:");
            foreach (string nodo in recorrido)
            {
                lstResultados.Items.Add(nodo);
            }
        }

        private void BtnRecorrerProfundidad_Click(object sender, EventArgs e)
        {
            if (cmbDepartamentos.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un departamento de inicio.");
                return;
            }

            string inicio = cmbDepartamentos.SelectedItem.ToString();
            List<string> recorrido = grafo.RecorridoProfundidad(inicio);

            lstResultados.Items.Clear();
            lstResultados.Items.Add($"Recorrido en Profundidad desde {inicio}:");
            foreach (string nodo in recorrido)
            {
                lstResultados.Items.Add(nodo);
            }
        }


    }

    public class Nodo
    {
        public string Departamento { get; set; }
        public string Cabecera { get; set; }
        public Point Ubicacion { get; set; }
        public List<Nodo> Vecinos { get; set; } = new List<Nodo>();

        public Nodo(string departamento, string cabecera, Point ubicacion)
        {
            Departamento = departamento;
            Cabecera = cabecera;
            Ubicacion = ubicacion;
        }
    }

    public class Grafo
    {
        public List<Nodo> Nodos { get; set; } = new List<Nodo>();

        public void AgregarNodo(string departamento, string cabecera, Point ubicacion)
        {
            Nodos.Add(new Nodo(departamento, cabecera, ubicacion));
        }

        public void ConectarNodos(string nombreOrigen, string nombreDestino)
        {
            Nodo origen = Nodos.Find(n => n.Departamento == nombreOrigen);
            Nodo destino = Nodos.Find(n => n.Departamento == nombreDestino);

            if (origen != null && destino != null)
            {
                if (!origen.Vecinos.Contains(destino))
                    origen.Vecinos.Add(destino);
                if (!destino.Vecinos.Contains(origen))
                    destino.Vecinos.Add(origen);
            }
        }

        public List<string> RecorridoAnchura(string inicio)
        {
            List<string> recorrido = new List<string>();
            Queue<Nodo> cola = new Queue<Nodo>();
            HashSet<string> visitados = new HashSet<string>();

            Nodo nodoInicio = Nodos.Find(n => n.Departamento == inicio);
            if (nodoInicio == null) return recorrido;

            cola.Enqueue(nodoInicio);
            visitados.Add(nodoInicio.Departamento);

            while (cola.Count > 0)
            {
                Nodo actual = cola.Dequeue();
                recorrido.Add(actual.Departamento);

                foreach (Nodo vecino in actual.Vecinos)
                {
                    if (!visitados.Contains(vecino.Departamento))
                    {
                        visitados.Add(vecino.Departamento);
                        cola.Enqueue(vecino);
                    }
                }
            }

            return recorrido;
        }

        public List<string> RecorridoProfundidad(string inicio)
        {
            List<string> recorrido = new List<string>();
            Stack<Nodo> pila = new Stack<Nodo>();
            HashSet<string> visitados = new HashSet<string>();

            Nodo nodoInicio = Nodos.Find(n => n.Departamento == inicio);
            if (nodoInicio == null) return recorrido;

            pila.Push(nodoInicio);
            visitados.Add(nodoInicio.Departamento);

            while (pila.Count > 0)
            {
                Nodo actual = pila.Pop();
                recorrido.Add(actual.Departamento);

                foreach (Nodo vecino in actual.Vecinos)
                {
                    if (!visitados.Contains(vecino.Departamento))
                    {
                        visitados.Add(vecino.Departamento);
                        pila.Push(vecino);
                    }
                }
            }

            return recorrido;
        }
    }
}