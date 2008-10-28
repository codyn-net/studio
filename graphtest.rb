require 'gtk2'
require 'graph'

$total = 0
$ds = 25

window = Gtk::Window.new
window.signal_connect('delete-event') do |win, lala|
	Gtk::main_quit
end

window.set_default_size(600, 200)
$graphs = (1..4).map { |x| Cpg::Graph.new($ds, $ds * 2, [-2, 2]) }

vbox = Gtk::VBox.new(false, 6)

$graphs.each { |x| vbox.pack_start(x, true, true, 0) }
window.add(vbox)

GLib::Timeout.add(1000 / $ds) do
	$graphs.each_with_index { |x, idx| x << Math::sin($total * 2 * Math::PI) + Math::sin($total * idx * Math::PI + 0.5 * Math::PI) }
	$total += 1.0 / $ds

	true
end

window.show_all

Gtk::main
