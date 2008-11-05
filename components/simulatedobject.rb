require 'components/gridobject'

module Cpg::Components
	class SimulatedObject < GridObject
		type_register

		property :integrate, :allocation
		read_only :integrate
		invisible :integrate, :allocation

		read_write :id
		visible :id
	
		def initialize
			super
		
			self.integrate = Cpg::Serialize::Array.new
			self.allocation = @allocation
		end
	
		def simulation_update(s)
			c = Cpg::MathContext.new(state)

			s.each do |prop, val|
				v = (integrated?(prop) ? c.eval(get_property(prop)).to_f : 0) + val
				set_property(prop, v)
			end
		end
	
		def simulation_step(dt)
			mystate = {}
		
			@links.each do |l| 
				s = l.simulation_evaluate
			
				s.each do |k,v|
					mystate[k] = (mystate[k] || 0) + v * (integrated?(k) ? dt : 1)
				end
			end
		
			mystate
		end
	
		def set_integrated(name, value)
			if value
				self.integrate << name unless self.integrate.include?(name)
			else
				self.integrate.delete(name) if self.integrate.include?(name)
			end
		end
		
		def integrated?(name)
			self.integrate.include?(name)
		end
	end
end
